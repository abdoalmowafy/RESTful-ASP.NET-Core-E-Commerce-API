using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Entities.Enums;
using Shopping.Customer.Contracts;
using Shopping.Customer.Services;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.ReviewTests;

public class ReviewServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;
    private readonly string _userId = Guid.NewGuid().ToString();

    public ReviewServiceTests()
    {
        _db = _sp.GetRequiredService<AppDbContext>();
    }

    private async Task<int> SeedAsync(bool delivered)
    {
        var category = TestData.Category();
        var product = TestData.Product(sku: "REV-0001", categoryId: 0);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        product.CategoryId = category.Id;
        _db.Products.Add(product);
        _db.Users.Add(new ApplicationUser
        {
            Id = _userId,
            FirstName = "Review",
            LastName = "Tester",
            Email = "reviewer@shop.test",
            UserName = "reviewer@shop.test",
            EmailConfirmed = true
        });

        var orderProduct = new OrderProduct { ProductId = product.Id, Quantity = 1, ProductPriceCents = 10_000 };
        var order = new Order
        {
            UserId = _userId,
            Status = delivered ? OrderStatus.Delivered : OrderStatus.OnTheWay,
            AddressId = 1,
            OrderProducts = [orderProduct]
        };

        if (delivered)
            order.DeliveredAt = DateTime.UtcNow.AddDays(-5);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return orderProduct.Id;
    }

    [Fact]
    public async Task Review_requires_a_delivered_purchase()
    {
        await SeedAsync(delivered: false);

        var result = await new ReviewService(_db).CreateAsync(_userId, 1, new ReviewRequest(4, "solid"));

        Assert.True(result.IsFailure);
        Assert.Equal(ShoppingErrors.Review.NotPurchased.Code, result.Error.Code);
    }

    [Fact]
    public async Task Review_after_delivery_is_accepted_once()
    {
        var orderProductId = await SeedAsync(delivered: true);
        var sut = new ReviewService(_db);

        var created = await sut.CreateAsync(_userId, 1, new ReviewRequest(4, "Great after delivery"));
        Assert.True(created.IsSucceed);
        Assert.Equal("Review Tester", created.Value.ReviewerName);

        var duplicate = await sut.CreateAsync(_userId, 1, new ReviewRequest(2, "again"));
        Assert.True(duplicate.IsFailure);
        Assert.Equal(ShoppingErrors.Review.AlreadyReviewed.Code, duplicate.Error.Code);
    }

    [Fact]
    public async Task Update_only_allows_the_review_owner_or_staff()
    {
        await SeedAsync(delivered: true);
        var sut = new ReviewService(_db);

        var review = await sut.CreateAsync(_userId, 1, new ReviewRequest(3, "initial"));

        var stranger = await sut.UpdateAsync(Guid.NewGuid().ToString(), review.Value.Id, new ReviewRequest(1, "hacked"), isStaff: false);
        Assert.True(stranger.IsFailure);
        Assert.Equal(ShoppingErrors.Review.Forbidden.Code, stranger.Error.Code);

        var staff = await sut.UpdateAsync(Guid.NewGuid().ToString(), review.Value.Id, new ReviewRequest(5, "moderated"), isStaff: true);
        Assert.True(staff.IsSucceed);
        Assert.Equal(5, staff.Value.Rating);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
