using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Persistence;
using Shopping.Customer.Contracts;
using Shopping.Customer.Services;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.ShoppingCartTests;

public class CartServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;
    private readonly string _userId = Guid.NewGuid().ToString();

    public CartServiceTests()
    {
        _db = _sp.GetRequiredService<AppDbContext>();
    }

    private async Task SeedAsync(params Product[] products)
    {
        var category = TestData.Category();
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        foreach (var p in products)
        {
            p.CategoryId = category.Id;
            _db.Products.Add(p);
        }
        _db.Carts.Add(new Cart { UserId = _userId });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task AddItem_merges_quantities_and_computes_totals_with_sale_percent()
    {
        await SeedAsync(
            TestData.Product(sku: "CRT-0001", quantity: 10, priceCents: 10_000, salePercent: 10),
            TestData.Product(sku: "CRT-0002", name: "Second", quantity: 5, priceCents: 5_000));
        var sut = new CartService(_db);

        await sut.AddItemAsync(_userId, new AddCartItemRequest(1, 2));
        var cart = await sut.AddItemAsync(_userId, new AddCartItemRequest(1, 1));
        cart = await sut.AddItemAsync(_userId, new AddCartItemRequest(2, 1));

        Assert.Equal(2, cart.Value.Items.Count);
        var first = cart.Value.Items.Single(i => i.ProductId == 1);
        Assert.Equal(3, first.Quantity);
        Assert.Equal(9_000, first.FinalPriceCents);
        Assert.Equal(27_000, first.LineTotalCents);
        Assert.Equal(32_000, cart.Value.SubtotalCents);
        Assert.Equal(32_000, cart.Value.TotalCents);
    }

    [Fact]
    public async Task AddItem_rejects_more_than_available_stock()
    {
        await SeedAsync(TestData.Product(sku: "CRT-0003", quantity: 3));
        var sut = new CartService(_db);

        var result = await sut.AddItemAsync(_userId, new AddCartItemRequest(1, 5));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.Product.OutOfStock.Code, result.Error.Code);
    }

    [Fact]
    public async Task ApplyPromo_caps_the_discount_at_max_sale_cents()
    {
        var promo = TestData.PromoCode(code: "CAP5", percent: 25, maxSaleCents: 500);
        await SeedAsync(TestData.Product(sku: "CRT-0004", quantity: 10, priceCents: 10_000));
        _db.PromoCodes.Add(promo);
        await _db.SaveChangesAsync();
        var sut = new CartService(_db);

        await sut.AddItemAsync(_userId, new AddCartItemRequest(1, 2));
        var cart = await sut.ApplyPromoAsync(_userId, new ApplyPromoRequest("cap5"));

        Assert.True(cart.IsSucceed);
        Assert.Equal("CAP5", cart.Value.PromoCode!.Code);
        Assert.Equal(20_000, cart.Value.SubtotalCents);
        Assert.Equal(500, cart.Value.DiscountCents);
        Assert.Equal(19_500, cart.Value.TotalCents);
    }

    [Fact]
    public async Task ApplyPromo_rejects_inactive_codes()
    {
        await SeedAsync(TestData.Product(sku: "CRT-0005"));
        _db.PromoCodes.Add(TestData.PromoCode(code: "DEAD", active: false));
        await _db.SaveChangesAsync();
        var sut = new CartService(_db);

        await sut.AddItemAsync(_userId, new AddCartItemRequest(1, 1));
        var result = await sut.ApplyPromoAsync(_userId, new ApplyPromoRequest("DEAD"));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.PromoCode.Inactive.Code, result.Error.Code);
    }

    [Fact]
    public async Task RemoveItem_deletes_the_line()
    {
        await SeedAsync(TestData.Product(sku: "CRT-0006"));
        var sut = new CartService(_db);

        await sut.AddItemAsync(_userId, new AddCartItemRequest(1, 2));
        var cart = await sut.RemoveItemAsync(_userId, 1);

        Assert.True(cart.IsSucceed);
        Assert.Empty(cart.Value.Items);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
