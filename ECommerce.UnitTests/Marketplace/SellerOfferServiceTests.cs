using ECommerce.Infrastructure.Caching;
using ECommerce.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Seller.Profile.Contracts;
using Seller.Profile.Services;

namespace ECommerce.UnitTests.Marketplace;

public class SellerOfferServiceTests : IDisposable
{
    private readonly IServiceProvider _sp;
    private readonly AppDbContext _db;
    private readonly HomePageCache _homeCache;

    public SellerOfferServiceTests()
    {
        _sp = TestHost.Build();
        TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
        _db = _sp.GetRequiredService<AppDbContext>();
        _homeCache = _sp.GetRequiredService<HomePageCache>();
    }

    public void Dispose()
    {
        _db.Dispose();
        (_sp as IDisposable)?.Dispose();
    }

    private async Task<string> SeedSellerAsync(string email = "offer-seller@shop.test")
    {
        var user = new ApplicationUser
        {
            FirstName = "Offery",
            LastName = "McOffer",
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };
        Assert.True((await _sp.GetRequiredService<UserManager<ApplicationUser>>().CreateAsync(user, "Passw0rd!")).Succeeded);
        return user.Id;
    }

    private SellerOfferService OfferSut() => new(_db, _homeCache);

    private async Task<(int StoreId, int ProductId)> SeedActiveStoreWithProductAsync(string ownerId)
    {
        await _db.Categories.AddAsync(TestData.Category());
        await _db.SaveChangesAsync();
        var categoryId = _db.Categories.First().Id;

        var store = await StoreSeed.CreateAsync(_db, ownerId, StoreStatus.Active, name: "Offer Store");
        var product = TestData.Product(sku: $"OFF-{Guid.NewGuid():N}"[..10], categoryId: categoryId);
        product.StoreId = store.Id;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return (store.Id, product.Id);
    }

    private static UpsertOfferRequest OfferRequest(params int[] productIds)
        => new(
            "Summer Blowout",
            "Save big",
            25,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(7),
            productIds);

    [Fact]
    public async Task Create_offer_on_active_store_succeeds_and_links_products()
    {
        var ownerId = await SeedSellerAsync();
        var (_, productId) = await SeedActiveStoreWithProductAsync(ownerId);
        var sut = OfferSut();

        var result = await sut.CreateAsync(ownerId, OfferRequest(productId), default);

        Assert.True(result.IsSucceed);
        Assert.Equal(25, result.Value.DiscountPercent);
        Assert.Equal([productId], result.Value.ProductIds);
    }

    [Fact]
    public async Task Create_offer_rejects_products_owned_by_another_store()
    {
        var ownerId = await SeedSellerAsync();
        var (_, _) = await SeedActiveStoreWithProductAsync(ownerId);

        var other = await SeedSellerAsync("other-owner@shop.test");
        var (_, otherProductId) = await SeedActiveStoreWithProductAsync(other);

        var sut = OfferSut();
        var result = await sut.CreateAsync(ownerId, OfferRequest(otherProductId), default);

        Assert.True(result.IsFailure);
        Assert.Equal(MarketplaceErrors.Offer.ProductNotOwned.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_offer_rejects_end_before_start()
    {
        var ownerId = await SeedSellerAsync();
        var (_, productId) = await SeedActiveStoreWithProductAsync(ownerId);
        var sut = OfferSut();

        var bad = OfferRequest(productId) with { EndsAt = DateTime.UtcNow.AddDays(-2) };
        var result = await sut.CreateAsync(ownerId, bad, default);

        Assert.True(result.IsFailure);
        Assert.Equal(MarketplaceErrors.Offer.InvalidDates.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_offer_rejects_empty_products()
    {
        var ownerId = await SeedSellerAsync();
        await SeedActiveStoreWithProductAsync(ownerId);
        var sut = OfferSut();

        var result = await sut.CreateAsync(ownerId, OfferRequest(), default);

        Assert.True(result.IsFailure);
        Assert.Equal(MarketplaceErrors.Offer.NoProducts.Code, result.Error.Code);
    }

    [Fact]
    public async Task Get_offers_is_scoped_to_owner()
    {
        var ownerId = await SeedSellerAsync();
        var (_, productId) = await SeedActiveStoreWithProductAsync(ownerId);
        var sut = OfferSut();
        await sut.CreateAsync(ownerId, OfferRequest(productId), default);

        var other = await SeedSellerAsync("viewer@shop.test");
        await SeedActiveStoreWithProductAsync(other);

        var mine = await sut.GetAsync(ownerId, 1, 10, default);
        Assert.True(mine.IsSucceed);
        Assert.Single(mine.Value.Items);
    }

    [Fact]
    public async Task SetActive_toggles_and_update_changes_discount()
    {
        var ownerId = await SeedSellerAsync();
        var (_, productId) = await SeedActiveStoreWithProductAsync(ownerId);
        var sut = OfferSut();

        var created = await sut.CreateAsync(ownerId, OfferRequest(productId), default);
        Assert.True(created.IsSucceed);

        var toggled = await sut.SetActiveAsync(ownerId, created.Value.Id, false, default);
        Assert.True(toggled.IsSucceed);

        var updated = await sut.UpdateAsync(ownerId, created.Value.Id,
            OfferRequest(productId) with { DiscountPercent = 40 }, default);
        Assert.True(updated.IsSucceed);
        Assert.Equal(40, updated.Value.DiscountPercent);
        Assert.False(updated.Value.IsActive);
    }

    [Fact]
    public async Task Delete_removes_offer()
    {
        var ownerId = await SeedSellerAsync();
        var (_, productId) = await SeedActiveStoreWithProductAsync(ownerId);
        var sut = OfferSut();

        var created = await sut.CreateAsync(ownerId, OfferRequest(productId), default);
        Assert.True(created.IsSucceed);

        var deleted = await sut.DeleteAsync(ownerId, created.Value.Id, default);
        Assert.True(deleted.IsSucceed);

        var get = await sut.GetAsync(ownerId, 1, 10, default);
        Assert.Empty(get.Value.Items);
    }
}
