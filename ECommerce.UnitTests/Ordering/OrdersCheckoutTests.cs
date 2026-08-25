using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Entities.Enums;
using Ordering.Customer.Contracts;
using Ordering.Customer.Services;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.CheckoutTests;

public sealed class FakePaymobService : IPaymobService
{
    public Task<Result<string>> PayAsync(Order order, string identifier, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Succeed("https://paymob.test/checkout"));
}

public class OrdersCheckoutTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;
    private FakeTrackingNotifier _notifier = new();
    private readonly string _userId = Guid.NewGuid().ToString();

    public OrdersCheckoutTests()
    {
        _db = _sp.GetRequiredService<AppDbContext>();
    }

    private async Task SeedAsync()
    {
        var category = TestData.Category();
        var product = TestData.Product(sku: "CHK-0001", quantity: 10, priceCents: 10_000, categoryId: 0);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        product.CategoryId = category.Id;
        _db.Products.Add(product);
        _db.Users.Add(new ApplicationUser
        {
            Id = _userId,
            FirstName = "Check",
            LastName = "Out",
            Email = "checkout@shop.test",
            UserName = "checkout@shop.test",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            PhoneNumber = "01000011111",
            Cart = new Cart
            {
                UserId = _userId,
                CartProducts = [new CartProduct { ProductId = product.Id, Quantity = 2 }]
            }
        });
        await _db.SaveChangesAsync();
    }

    private async Task<OrdersService> CreateSutAsync(Address? address = null)
    {
        _db.Addresses.AddRange(address ?? TestData.CustomerAddress(_userId), TestData.StoreAddress());
        await _db.SaveChangesAsync();
        var notifier = new FakeTrackingNotifier();
        _notifier = notifier;
        return new OrdersService(_db, new FakePaymobService(), notifier);
    }

    [Fact]
    public async Task Checkout_with_empty_cart_fails()
    {
        await SeedAsync();
        _db.CartProducts.RemoveRange(_db.CartProducts);
        await _db.SaveChangesAsync();
        var sut = await CreateSutAsync();

        var result = await sut.CheckoutAsync(_userId, new CheckoutRequest(1, true, PaymentMethod.COD));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderingErrors.Order.EmptyCart.Code, result.Error.Code);
    }

    [Fact]
    public async Task Checkout_blocks_a_second_ongoing_order()
    {
        await SeedAsync();
        _db.Orders.Add(new Order { UserId = _userId, Status = OrderStatus.Processing, AddressId = 1 });
        await _db.SaveChangesAsync();
        var sut = await CreateSutAsync();

        var result = await sut.CheckoutAsync(_userId, new CheckoutRequest(1, true, PaymentMethod.COD));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderingErrors.Order.OngoingExists.Code, result.Error.Code);
    }

    [Fact]
    public async Task COD_checkout_applies_promo_fees_stock_and_clears_the_cart()
    {
        await SeedAsync();
        var promo = TestData.PromoCode(code: "PCT10", percent: 10);
        _db.PromoCodes.Add(promo);
        await _db.SaveChangesAsync();

        var cart = await _db.Carts.Include(c => c.CartProducts).FirstAsync(c => c.UserId == _userId);
        cart.PromoCodeId = promo.Id;

        var sut = await CreateSutAsync();

        var result = await sut.CheckoutAsync(_userId, new CheckoutRequest(1, true, PaymentMethod.COD));

        Assert.True(result.IsSucceed);

        var orderProduct = await _db.OrderProducts.Include(op => op.Order).FirstAsync(op => op.Order!.UserId == _userId);
        var order = orderProduct.Order!;

        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Equal(24_000, order.TotalCents);
        Assert.Single(order.StatusEvents);
        var timelineEvent = order.StatusEvents.Single();
        Assert.Equal(OrderStatus.Processing, timelineEvent.Status);
        Assert.Equal("COD order placed", timelineEvent.Note);
        Assert.Single(_notifier.StatusCalls);
        Assert.Equal(OrderStatus.Processing, _notifier.StatusCalls[0].Status);

        var product = await _db.Products.FirstAsync(p => p.Sku == "CHK-0001");
        Assert.Equal(8, product.Quantity);

        var cartAfter = await _db.Carts.Include(c => c.CartProducts).FirstAsync(c => c.UserId == _userId);
        Assert.Empty(cartAfter.CartProducts);
        Assert.Null(cartAfter.PromoCodeId);
    }

    [Fact]
    public async Task Store_pickup_skips_the_delivery_fee()
    {
        await SeedAsync();
        var sut = await CreateSutAsync();
        var storeAddress = await _db.Addresses.AsNoTracking().FirstAsync(a => a.UserId == null);

        var result = await sut.CheckoutAsync(_userId, new CheckoutRequest(storeAddress.Id, false, PaymentMethod.COD));

        Assert.True(result.IsSucceed, $"unexpected error: {result.Error.Code}: {result.Error.Description}");
        var order = await _db.Orders.FirstAsync(o => o.UserId == _userId);
        Assert.False(order.DeliveryNeeded);
        Assert.Equal(21_000, order.TotalCents);
    }

    [Fact]
    public async Task Online_payment_returns_a_checkout_url_and_keeps_paying_status()
    {
        await SeedAsync();
        var sut = await CreateSutAsync();

        var result = await sut.CheckoutAsync(_userId, new CheckoutRequest(1, true, PaymentMethod.CreditCard, "01000011111"));

        Assert.True(result.IsSucceed);
        Assert.Null(result.Value.Order);
        Assert.Contains("paymob.test", result.Value.PaymentUrl);

        var order = await _db.Orders.FirstAsync(o => o.UserId == _userId);
        Assert.Equal(OrderStatus.Paying, order.Status);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
