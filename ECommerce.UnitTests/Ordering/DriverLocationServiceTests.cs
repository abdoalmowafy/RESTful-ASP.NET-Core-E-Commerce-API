using ECommerce.Infrastructure.Entities;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Ordering;

public class DriverLocationServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;
    private readonly string _driverId = Guid.NewGuid().ToString();
    private readonly string _customerId = Guid.NewGuid().ToString();

    public DriverLocationServiceTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
        _db = _sp.GetRequiredService<AppDbContext>();
    }

    private UserManager<ApplicationUser> _users;

    private async Task<int> SeedOnTheWayOrderAsync(double? destLat = 30.05, double? destLng = 31.23)
    {
        var address = TestData.CustomerAddress(_customerId);
        address.Latitude = destLat;
        address.Longitude = destLng;

        var category = TestData.Category();
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var product = TestData.Product(sku: "LOC-0001", categoryId: category.Id);
        _db.Products.Add(product);

        var driver = new ApplicationUser
        {
            Id = _driverId,
            FirstName = "Dan",
            LastName = "Driver",
            Email = "loc-driver@shop.test",
            UserName = "loc-driver@shop.test",
            EmailConfirmed = true
        };
        var customer = new ApplicationUser
        {
            Id = _customerId,
            FirstName = "Cust",
            LastName = "Omer",
            Email = "loc-customer@shop.test",
            UserName = "loc-customer@shop.test",
            EmailConfirmed = true
        };

        _db.Users.AddRange(driver, customer);
        await _db.SaveChangesAsync();

        _db.Orders.Add(new Order
        {
            UserId = _customerId,
            TransporterId = _driverId,
            Status = OrderStatus.OnTheWay,
            DeliveryNeeded = true,
            AddressId = address.Id,
            Address = address,
            TotalCents = 10_000
        });

        await _contextSaveAsync();
        return (await _db.Orders.FirstAsync(o => o.TransporterId == _driverId)).Id;
    }

    private Task _contextSaveAsync() => _db.SaveChangesAsync();

    [Fact]
    public async Task Ping_by_assigned_driver_is_stored_and_returned()
    {
        var orderId = await SeedOnTheWayOrderAsync();
        var sut = new DriverLocationService(_db, _cache());

        var ping = await sut.PingAsync(_driverId, orderId, 30.01, 31.20);
        Assert.True(ping.IsSucceed);

        var latest = await sut.GetLatestAsync(orderId);
        Assert.True(latest.IsSucceed);
        Assert.Equal(30.01, latest.Value.Point.Latitude, 5);
        Assert.NotNull(latest.Value.EtaMinutes);
    }

    [Fact]
    public async Task Ping_by_non_assigned_driver_is_rejected()
    {
        var orderId = await SeedOnTheWayOrderAsync();

        var result = await new DriverLocationService(_db, _cache())
            .PingAsync(Guid.NewGuid().ToString(), orderId, 30.0, 31.0);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderingErrors.Order.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Delivered_order_no_longer_accepts_or_serves_location()
    {
        var orderId = await SeedOnTheWayOrderAsync();
        var sut = new DriverLocationService(_db, _cache());
        await sut.PingAsync(_driverId, orderId, 30.01, 31.20);

        var order = await _db.Orders.FirstAsync(o => o.Id == orderId);
        order.Status = OrderStatus.Delivered;
        await _db.SaveChangesAsync();

        Assert.True((await sut.PingAsync(_driverId, orderId, 30.02, 31.21)).IsFailure);
        Assert.True((await sut.GetLatestAsync(orderId)).IsFailure);
    }

    [Fact]
    public void Haversine_distance_between_known_points_is_correct()
    {
        var km = DriverLocationService.HaversineKm(30.0444, 31.2357, 29.9870, 31.2118); // Cairo -> Giza-ish
        Assert.InRange(km, 5, 9);
    }

    private ECommerce.Infrastructure.Services.CacheService _cache()
        => new(new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions())));

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
