using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.UnitTests.Security;

/// <summary>
/// The money path, over real HTTP against the real host:
/// register → (confirm) → login → address → cart → COD checkout → Processing order.
/// </summary>
public class MoneyPathSmokeTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public MoneyPathSmokeTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.InitializeDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static StringContent Json(object o)
        => new(System.Text.Json.JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Customer_can_complete_a_cod_order_end_to_end()
    {
        var client = _factory.CreateClient();

        // register
        var register = await client.PostAsync("/api/auth/register", Json(new
        {
            firstName = "Money",
            lastName = "Path",
            email = "money@e2e.test",
            password = "Passw0rd!",
            phoneNumber = "01055500001"
        }));
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());

        // registration leaves contacts unconfirmed — confirm directly (OTP flow covered elsewhere)
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync("money@e2e.test");
            user!.EmailConfirmed = true;
            user.PhoneNumberConfirmed = true;
            await users.UpdateAsync(user);
        }

        // login
        var login = await client.PostAsync("/api/auth/login", Json(new { email = "money@e2e.test", password = "Passw0rd!" }));
        Assert.True(login.IsSuccessStatusCode);
        var auth = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.GetProperty("token").GetString());

        // seed catalog + category via scope
        int productId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new ECommerce.Infrastructure.Entities.Category { Name = "MoneyPath" };
            db.Categories.Add(category);

            var sellerId = Guid.NewGuid().ToString();
            db.Users.Add(new ApplicationUser
            {
                Id = sellerId,
                FirstName = "Seed",
                LastName = "Seller",
                Email = "seed-seller@e2e.test",
                UserName = "seed-seller@e2e.test",
                EmailConfirmed = true
            });
            db.Stores.Add(new ECommerce.Infrastructure.Entities.Store
            {
                OwnerId = sellerId,
                Name = "Seed Store",
                Slug = $"seed-{sellerId[..8]}",
                Status = StoreStatus.Active
            });
            await db.SaveChangesAsync();

            db.Products.Add(new ECommerce.Infrastructure.Entities.Product
            {
                Name = "E2E Gadget",
                Sku = "E2E-0001",
                Description = "gadget",
                CategoryId = category.Id,
                StoreId = 1,
                Quantity = 5,
                PriceCents = 12_345,
                WarrantyDays = 30
            });
            await db.SaveChangesAsync();

            productId = (await db.Products.FirstAsync(p => p.Sku == "E2E-0001")).Id;
        }

        // add to cart + create address
        Assert.True((await client.PostAsync("/api/cart/items", Json(new { productId, quantity = 1 }))).IsSuccessStatusCode);
        var address = await client.PostAsync("/api/addresses", Json(new
        {
            apartment = "1", floor = "1", building = "B", street = "S",
            city = "Cairo", state = "C", country = "EG", postalCode = "123"
        }));
        Assert.True(address.IsSuccessStatusCode, await address.Content.ReadAsStringAsync());
        var addrId = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            await address.Content.ReadAsStringAsync()).GetProperty("id").GetInt32();

        // checkout COD with delivery
        var checkout = await client.PostAsync("/api/orders/checkout", Json(new
        {
            addressId = addrId,
            deliveryNeeded = true,
            paymentMethod = "COD"
        }));

        Assert.True(checkout.IsSuccessStatusCode, await checkout.Content.ReadAsStringAsync());
        var order = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            await checkout.Content.ReadAsStringAsync()).GetProperty("order");

        Assert.Equal("Processing", order.GetProperty("status").GetString());

        var items = order.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(12_345 + 5000 /*delivery*/ + 1000 /*COD*/, order.GetProperty("totalCents").GetInt64());
    }
}
