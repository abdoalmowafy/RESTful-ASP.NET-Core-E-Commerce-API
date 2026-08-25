using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;

namespace ECommerce.Infrastructure.Services;

public interface IDriverLocationService
{
    Task<Result> PingAsync(string driverId, int orderId, double latitude, double longitude, CancellationToken cancellationToken = default);
    Task<Result<(DriverLocationPoint Point, int? EtaMinutes)>> GetLatestAsync(int orderId, CancellationToken cancellationToken = default);
}

public class DriverLocationService(AppDbContext context, ICacheService cache) : IDriverLocationService
{
    private const double AverageCitySpeedKmh = 22;

    private readonly AppDbContext _context = context;
    private readonly ICacheService _cache = cache;
    private static string Key(int orderId) => $"tracking:order:{orderId}";

    public async Task<Result> PingAsync(string driverId, int orderId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.DeletedAt == null, cancellationToken);

        if (order is null || order.TransporterId != driverId || !order.DeliveryNeeded)
            return Result.Failure(OrderingErrors.Order.NotFound);

        if (order.Status is not (OrderStatus.Processing or OrderStatus.OnTheWay))
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        var point = new DriverLocationPoint(latitude, longitude, DateTime.UtcNow);
        await _cache.SetAsync(Key(orderId), point, TimeSpan.FromMinutes(15), cancellationToken);

        return Result.Succeed();
    }

    public async Task<Result<(DriverLocationPoint Point, int? EtaMinutes)>> GetLatestAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Address)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.DeletedAt == null, cancellationToken);

        if (order?.Address is null)
            return Result.Failure<(DriverLocationPoint, int?)>(OrderingErrors.Order.NotFound);

        if (order.Status != OrderStatus.OnTheWay)
            return Result.Failure<(DriverLocationPoint, int?)>(Error.Conflict("Tracking.NotOnTheWay", "Live location is only available while the order is on the way"));

        var point = await _cache.GetAsync<DriverLocationPoint>(Key(orderId), cancellationToken);
        if (point is null)
            return Result.Failure<(DriverLocationPoint, int?)>(Error.NotFound("Tracking.NoLocation", "No live location has been received yet"));

        int? etaMinutes = null;
        if (order.Address.Latitude.HasValue && order.Address.Longitude.HasValue)
        {
            var km = HaversineKm(point.Latitude, point.Longitude, order.Address.Latitude.Value, order.Address.Longitude.Value);
            etaMinutes = Math.Max(1, (int)Math.Ceiling(km / AverageCitySpeedKmh * 60));
        }

        return Result.Succeed((point, etaMinutes));
    }

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371;
        double ToRad(double degrees) => degrees * Math.PI / 180;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);

        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(a));
    }
}
