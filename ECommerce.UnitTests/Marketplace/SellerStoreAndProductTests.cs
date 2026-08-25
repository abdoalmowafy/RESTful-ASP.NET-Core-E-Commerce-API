using ECommerce.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Seller.Profile.Contracts;
using Seller.Profile.Services;

namespace ECommerce.UnitTests.Marketplace;

public class SellerStoreAndProductTests : IDisposable
{
    private readonly IServiceProvider _sp;
    private readonly AppDbContext _db;

    public SellerStoreAndProductTests()
    {
        _sp = TestHost.Build();
        TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
        _db = _sp.GetRequiredService<AppDbContext>();
    }

    private async Task<string> SeedSellerAsync(string email = "seller1@shop.test")
    {
        var user = new ApplicationUser
        {
            FirstName = "Selly",
            LastName = "McSellface",
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };
        Assert.True((await userManager().CreateAsync(user, "Passw0rd!")).Succeeded);
        return user.Id;
    }

    private UserManager<ApplicationUser> userManager() => _sp.GetRequiredService<UserManager<ApplicationUser>>();

    private async Task<SellerProductService> ProductSutAsync(string ownerId)
    {
        await _db.Categories.AddAsync(TestData.Category());
        await _db.SaveChangesAsync();

        var storageRoot = Path.Combine(Path.GetTempPath(), $"ec-seller-{Guid.NewGuid():N}");
        IHostEnvironment env = new FakeWebHostEnvironment { ContentRootPath = AppContext.BaseDirectory, WebRootPath = storageRoot };
        var storage = new LocalFileStorage((IWebHostEnvironment)env, Options.Create(new FileStorageOptions { RootPath = storageRoot }));

        return new SellerProductService(_db, storage);
    }

    [Fact]
    public async Task Create_store_starts_pending_and_blocks_selling_until_approved()
    {
        var ownerId = await SeedSellerAsync();
        var storeSut = new SellerStoreService(_db, userManager());

        var created = await storeSut.CreateAsync(ownerId, new UpsertStoreRequest("Gadget Garage", "electronics", null));
        Assert.Equal(StoreStatus.PendingVerification, created.Value.Status);

        var productSut = await ProductSutAsync(ownerId);
        var blocked = await productSut.CreateAsync(ownerId,
            new SellerProductRequest("GPU", "SEL-0001", "graphics card", 1, 5, 100_00, 0, 14), [], default);

        Assert.True(blocked.IsFailure);
        Assert.Equal(MarketplaceErrors.Store.NotActive.Code, blocked.Error.Code);
    }

    [Fact]
    public async Task Approved_store_can_create_products_scoped_to_its_store()
    {
        var ownerId = await SeedSellerAsync();
        var store = await StoreSeed.CreateAsync(_db, ownerId);
        var productSut = await ProductSutAsync(ownerId);

        var created = await productSut.CreateAsync(ownerId,
            new SellerProductRequest("Mechanical Keyboard", "SEL-0002", "clicky keys", 1, 7, 25_00, 10, 365), [], default);

        Assert.True(created.IsSucceed);
        var product = await _db.Products.Include(p => p.Store).FirstAsync(p => p.Sku == "SEL-0002");
        Assert.Equal(store.Id, product.StoreId);
    }

    [Fact]
    public async Task Sellers_cannot_touch_other_stores_products()
    {
        var ownerA = await SeedSellerAsync("ownerA@shop.test");
        var ownerB = await SeedSellerAsync("ownerB@shop.test");
        var storeA = await StoreSeed.CreateAsync(_db, ownerA, name: "Store A");
        var storeB = await StoreSeed.CreateAsync(_db, ownerB, name: "Store B");

        await _db.Categories.AddAsync(TestData.Category());
        await _db.SaveChangesAsync();

        var product = TestData.Product(sku: "OWN-0001", categoryId: 1);
        product.StoreId = storeA.Id;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var sutB = await ProductSutAsync(ownerB);

        var updateByB = await sutB.UpdateAsync(ownerB, product.Id,
            new SellerProductRequest("Hijacked", "OWN-0001", "x", 1, 1, 1_00, 0, 14), default);
        Assert.True(updateByB.IsFailure);

        var deleteByB = await sutB.DeleteAsync(ownerB, product.Id, TestActor.For(ownerB), default);
        Assert.True(deleteByB.IsFailure);

        var stockByB = await sutB.SetStockAsync(ownerB, product.Id, new SellerStockRequest(99), default);
        Assert.True(stockByB.IsFailure);

        var untouched = await _db.Products.AsNoTracking().FirstAsync(p => p.Sku == "OWN-0001");
        Assert.Equal(storeA.Id, untouched.StoreId);
        Assert.NotEqual(99, untouched.Quantity);
    }

    [Fact]
    public async Task Second_store_for_the_same_owner_is_rejected()
    {
        var ownerId = await SeedSellerAsync();
        await StoreSeed.CreateAsync(_db, ownerId);
        var sut = new SellerStoreService(_db, userManager());

        var result = await sut.CreateAsync(ownerId, new UpsertStoreRequest("Another One", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(MarketplaceErrors.Store.AlreadyExists.Code, result.Error.Code);
    }

    [Fact]
    public async Task Creating_first_store_promotes_owner_to_seller_role()
    {
        var ownerId = await SeedSellerAsync();
        var sut = new SellerStoreService(_db, userManager());

        var created = await sut.CreateAsync(ownerId, new UpsertStoreRequest("Promo Store", null, null));
        Assert.True(created.IsSucceed);

        var owner = await userManager().FindByIdAsync(ownerId);
        Assert.Contains(DefaultRoles.Seller, await userManager().GetRolesAsync(owner!));
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
