using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ECommerce.UnitTests.Infrastructure;

/// <summary>
/// Boots the REAL Store.API host (all modules wired) against a throwaway
/// PostgreSQL database — same engine as production, so FTS, xmin concurrency
/// and migrations behave exactly like they will in deployment.
/// Database create/migrate/seed happens once; dropped on dispose.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"ec_test_{Guid.NewGuid():N}";
    private long _initialized;

    public string ConnectionString => $"Host=localhost;Port=5433;Database={_dbName};Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Jwt:Key", "integration-test-signing-key-0123456789abcdef-0123456789abcdef");
        builder.UseSetting("Jwt:Issuer", "Store.API");
        builder.UseSetting("Jwt:Audience", "Store.Client");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", "localhost:6379");
        builder.UseSetting("DataRetention:Enabled", "false");
    }

    public async Task InitializeDatabaseAsync(Action<IServiceProvider>? extraSeed = null)
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        await using (var admin = new NpgsqlConnection("Host=localhost;Port=5433;Username=postgres;Password=postgres"))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = $"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE);";
            await cmd.ExecuteNonQueryAsync();
        }

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            await SeedIdentityAsync(scope.ServiceProvider);

            if (extraSeed is not null)
                extraSeed(scope.ServiceProvider);
        }
    }

    private async Task SeedIdentityAsync(IServiceProvider sp)
    {
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var db = sp.GetRequiredService<AppDbContext>();

        foreach (var (name, perms) in new Dictionary<string, string[]>
        {
            [DefaultRoles.SuperAdmin] = Permissions.All,
            [DefaultRoles.Admin] = Permissions.All,
            [DefaultRoles.Customer] = Array.Empty<string>(),
            [DefaultRoles.Seller] = Array.Empty<string>(),
            [DefaultRoles.Driver] = new[] { Permissions.Deliveries.Handle }
        })
        {
            if (await roles.FindByNameAsync(name) is null)
                await roles.CreateAsync(new ApplicationRole { Name = name, IsDefault = true });

            var role = (await roles.FindByNameAsync(name))!;
            var existing = (await roles.GetClaimsAsync(role))
                .Where(cl => cl.Type == "permission")
                .Select(cl => cl.Value)
                .ToList();

            foreach (var perm in perms.Where(p => !existing.Contains(p)))
                await roles.AddClaimAsync(role, new System.Security.Claims.Claim("permission", perm));
        }

        foreach (var (email, firstName, lastName, role) in new[]
        {
            ("superadmin@matrix.test", "Super", "Admin", DefaultRoles.SuperAdmin),
            ("admin@matrix.test", "Adam", "Admin", DefaultRoles.Admin),
            ("customer@matrix.test", "Casey", "Customer", DefaultRoles.Customer),
            ("seller@matrix.test", "Sam", "Seller", DefaultRoles.Seller),
            ("driver@matrix.test", "Danny", "Driver", DefaultRoles.Driver)
        })
        {
            if (await users.FindByEmailAsync(email) is not null)
                continue;

            var user = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                PhoneNumber = "01000000000"
            };

            Assert.True((await users.CreateAsync(user, "Passw0rd!")).Succeeded);
            await users.AddToRoleAsync(user, role);

            switch (role)
            {
                case DefaultRoles.Customer:
                    user.CustomerProfile = new CustomerProfile { Id = user.Id };
                    break;

                case DefaultRoles.Seller:
                    var store = new Store
                    {
                        OwnerId = user.Id,
                        Name = $"{firstName} Store",
                        Slug = $"store-{user.Id[..8]}",
                        Status = StoreStatus.Active
                    };
                    db.Stores.Add(store);
                    await db.SaveChangesAsync();
                    user.SellerProfile = new SellerProfile { Id = user.Id, StoreId = store.Id };
                    break;

                case DefaultRoles.Driver:
                    user.DriverProfile = new DriverProfile
                    {
                        Id = user.Id,
                        Status = DriverStatus.Active,
                        PlateNumber = "TST 0001",
                        LicenseNumber = "DL-0001"
                    };
                    break;
            }

            await users.UpdateAsync(user);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                NpgsqlConnection.ClearAllPools();

                using var admin = new NpgsqlConnection("Host=localhost;Port=5433;Username=postgres;Password=postgres");
                admin.Open();
                using var cmd = admin.CreateCommand();
                cmd.CommandText = $"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE);";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // best effort on shutdown
            }
        }

        base.Dispose(disposing);
    }
}
