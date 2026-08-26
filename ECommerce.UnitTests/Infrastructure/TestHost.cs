using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace ECommerce.UnitTests.Infrastructure;

public static class TestHost
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging(o => o.SetMinimumLevel(LogLevel.Warning));
        services.AddHttpContextAccessor();

        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"tests-{Guid.NewGuid():N}"));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    public static async Task<(UserManager<ApplicationUser> Users, RoleManager<ApplicationRole> Roles)> CreateIdentityAsync(IServiceProvider sp)
    {
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var roleName in new[] { "SuperAdmin", "Admin", "Customer", "Seller", "Driver" })
        {
            if (await roles.FindByNameAsync(roleName) is null)
                await roles.CreateAsync(new ApplicationRole { Name = roleName });
        }

        return (users, roles);
    }
}

public sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "ECommerce.UnitTests";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), $"ec-tests-{Guid.NewGuid():N}");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
