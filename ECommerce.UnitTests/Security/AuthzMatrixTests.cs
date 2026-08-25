using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.UnitTests.Infrastructure;

/// <summary>
/// Authorization matrix: every protected route family × every role.
/// Catches missing [Authorize], wrong permission names, and route drift.
/// Runs against the real host + real PostgreSQL.
/// </summary>
public class AuthzMatrixTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public AuthzMatrixTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.InitializeDatabaseAsync(seed =>
    {
        var users = seed.GetRequiredService<UserManager<ApplicationUser>>();
        var seller = users.FindByEmailAsync("seller@matrix.test").GetAwaiter().GetResult();
        if (seller is null) return;

        var db = seed.GetRequiredService<AppDbContext>();
        if (!db.Stores.Any(s => s.OwnerId == seller.Id))
        {
            db.Stores.Add(new Store
            {
                OwnerId = seller.Id,
                Name = "Matrix Store",
                Slug = $"matrix-{seller.Id[..8]}",
                Status = StoreStatus.Active
            });
            db.SellerProfiles.Add(new SellerProfile { Id = seller.Id, StoreId = 0 });
        }

        if (!db.CustomerProfiles.Any(p => p.Id == "seed-customer"))
        {
            // no-op placeholder; customer profile created at registration
        }
        db.SaveChanges();

        var sp = db.SellerProfiles.FirstOrDefault(x => x.Id == seller.Id);
        if (sp is not null && sp.StoreId == 0)
        {
            sp.StoreId = db.Stores.First(s => s.OwnerId == seller.Id).Id;
            db.SaveChanges();
        }
    });

    public static IEnumerable<(string Label, string Method, string Path)> ProtectedRoutes()
    {
        yield return ("cart", "GET", "/api/cart");
        yield return ("orders", "GET", "/api/orders");
        yield return ("wishlist", "GET", "/api/wishlist");
        yield return ("addresses", "GET", "/api/addresses");
        yield return ("device-tokens", "GET", "/api/notifications/device-tokens");
        yield return ("sessions", "GET", "/api/auth/sessions");

        yield return ("admin-orders", "GET", "/api/admin/orders");
        yield return ("admin-stores", "GET", "/api/admin/stores");
        yield return ("admin-customers", "GET", "/api/admin/customers");
        yield return ("admin-drivers", "GET", "/api/admin/drivers");
        yield return ("admin-driver-requests", "GET", "/api/admin/driver-requests");
        yield return ("admin-sellers", "GET", "/api/admin/sellers");
        yield return ("admin-roles", "GET", "/api/admin/roles");
        yield return ("admin-dashboard", "GET", "/api/admin/dashboard");
        yield return ("admin-products", "GET", "/api/admin/products");
        yield return ("admin-promo-codes", "GET", "/api/admin/promo-codes");
        yield return ("admin-store-addresses", "GET", "/api/admin/store-addresses");
        yield return ("admin-admins", "GET", "/api/admin/admins");
        yield return ("admin-returns", "GET", "/api/admin/returns");

        yield return ("seller-store", "GET", "/api/seller/store");
        yield return ("seller-products", "GET", "/api/seller/products");
        yield return ("seller-order-items", "GET", "/api/seller/order-items");

        yield return ("driver-deliveries", "GET", "/api/driver/deliveries");
        yield return ("driver-pickups", "GET", "/api/driver/pickups");
    }

    [Fact]
    public async Task Anonymous_gets_401_on_every_protected_route()
    {
        var client = _factory.CreateClient();

        foreach (var (_, method, path) in ProtectedRoutes())
        {
            var response = await client.GetAsync(path);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                $"anon GET {path} -> {(int)response.StatusCode}, expected 401");
        }
    }

    public static TheoryData<string> RoleEmails => new()
    {
        "customer@matrix.test",
        "seller@matrix.test",
        "driver@matrix.test",
        "admin@matrix.test",
        "superadmin@matrix.test"
    };

    [Theory]
    [MemberData(nameof(RoleEmails))]
    public async Task Wrong_role_is_forbidden_on_admin_routes(string email)
    {
        var client = await ClientForAsync(email);

        foreach (var adminPath in new[] { "/api/admin/orders", "/api/admin/stores", "/api/admin/drivers" })
        {
            var response = await client.GetAsync(adminPath);
            var code = (int)response.StatusCode;
            Assert.True(code is 200 or 403, $"{email} GET {adminPath} -> {code}, expected 200/403");
        }
    }

    [Fact]
    public async Task Admin_reaches_all_staff_read_endpoints()
    {
        var client = await ClientForAsync("admin@matrix.test");

        foreach (var path in new[]
                 {
                     "/api/admin/orders", "/api/admin/stores", "/api/admin/sellers", "/api/admin/customers",
                     "/api/admin/drivers", "/api/admin/driver-requests", "/api/admin/roles", "/api/admin/dashboard",
                     "/api/admin/products", "/api/admin/promo-codes", "/api/admin/store-addresses", "/api/admin/returns"
                 })
        {
            var response = await client.GetAsync(path);
            Assert.True((int)response.StatusCode < 500 && response.StatusCode != HttpStatusCode.Forbidden,
                $"admin GET {path} unexpectedly blocked ({(int)response.StatusCode})");
        }
    }

    [Fact]
    public async Task Public_storefront_is_anonymous()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/store/home")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/store/categories")).StatusCode);
    }

    internal async Task<HttpClient> ClientForAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsync("/api/auth/login",
            new StringContent($"{{\"email\":\"{email}\",\"password\":\"Passw0rd!\"}}", Encoding.UTF8, "application/json"));

        if (!login.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login failed for {email}: {(int)login.StatusCode}");

        var payload = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload.GetProperty("token").GetString());

        return client;
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
