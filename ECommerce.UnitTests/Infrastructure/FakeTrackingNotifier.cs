using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Services;

namespace ECommerce.UnitTests.Infrastructure;

public sealed class FakeTrackingNotifier : IOrderTrackingNotifier
{
    public List<(int OrderId, OrderStatus Status, string? Note)> StatusCalls { get; } = [];
    public List<(int OrderId, double Lat, double Lng)> LocationCalls { get; } = [];

    public Task NotifyStatusAsync(int orderId, OrderStatus status, string? note, DateTime occurredAt)
    {
        StatusCalls.Add((orderId, status, note));
        return Task.CompletedTask;
    }

    public Task NotifyDriverLocationAsync(int orderId, double latitude, double longitude, DateTime recordedAt, int? etaMinutes)
    {
        LocationCalls.Add((orderId, latitude, longitude));
        return Task.CompletedTask;
    }
}
