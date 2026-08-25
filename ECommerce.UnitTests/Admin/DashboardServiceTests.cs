using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;
using Admin.Management.Contracts;
using Admin.Management.Services;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.DashboardTests;

public class DashboardServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;

    public DashboardServiceTests()
    {
        _db = _sp.GetRequiredService<AppDbContext>();
    }

    private async Task SeedAsync()
    {
        var category = TestData.Category();
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        _db.Products.AddRange(
            TestData.Product(sku: "DSH-LOW1", name: "Low A", quantity: 2, categoryId: category.Id),
            TestData.Product(sku: "DSH-OK", name: "Healthy", quantity: 500, categoryId: category.Id));

        var customer = new ApplicationUser
        {
            FirstName = "Dash",
            LastName = "Customer",
            Email = "dash@shop.test",
            UserName = "dash@shop.test",
            EmailConfirmed = true
        };
        _db.Users.Add(customer);
        await _db.SaveChangesAsync();

        _db.Addresses.Add(TestData.CustomerAddress(customer.Id));
        await _db.SaveChangesAsync();
        var addressId = (await _db.Addresses.FirstAsync(a => a.UserId == customer.Id)).Id;

        _db.Orders.AddRange(
            new Order { UserId = customer.Id, Status = OrderStatus.Delivered, TotalCents = 10_000, AddressId = addressId },
            new Order { UserId = customer.Id, Status = OrderStatus.Processing, TotalCents = 5_000, AddressId = addressId },
            new Order { UserId = customer.Id, Status = OrderStatus.Cancelled, TotalCents = 99_999, AddressId = addressId });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Dashboard_aggregates_revenue_excluding_cancelled_orders()
    {
        await SeedAsync();

        var result = await new DashboardService(_db).GetAsync();

        Assert.True(result.IsSucceed);
        Assert.Equal(15_000, result.Value.RevenueCents);
        Assert.Equal(15_000, result.Value.RevenueLast30DaysCents);
        Assert.Equal(2, result.Value.ActiveOrders);
        Assert.Single(result.Value.Last14Days, d => d.Orders > 0);
    }

    [Fact]
    public async Task Dashboard_lists_low_stock_products_only()
    {
        await SeedAsync();

        var result = await new DashboardService(_db).GetAsync();

        var lowStock = Assert.Single(result.Value.LowStock);
        Assert.Equal("Low A", lowStock.Name);
        Assert.Equal(2, lowStock.Quantity);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
