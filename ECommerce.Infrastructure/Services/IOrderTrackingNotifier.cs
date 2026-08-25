using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Services;

public interface IOrderTrackingNotifier
{
    Task NotifyStatusAsync(int orderId, OrderStatus status, string? note, DateTime occurredAt);
    Task NotifyDriverLocationAsync(int orderId, double latitude, double longitude, DateTime recordedAt, int? etaMinutes);
}

public record DriverLocationPoint(double Latitude, double Longitude, DateTime RecordedAt)
{
    public bool IsStale(DateTime now) => now - RecordedAt > TimeSpan.FromMinutes(10);
}
