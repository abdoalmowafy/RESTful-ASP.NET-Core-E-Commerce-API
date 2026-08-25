using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.Infrastructure.Services;

public class SignalROrderTrackingNotifier(IHubContext<TrackingHub> hubContext) : IOrderTrackingNotifier
{
    private readonly IHubContext<TrackingHub> _hubContext = hubContext;

    public async Task NotifyStatusAsync(int orderId, OrderStatus status, string? note, DateTime occurredAt)
        => await _hubContext.Clients
            .Group(TrackingHub.GroupName(orderId))
            .SendAsync("orderStatusChanged", new
            {
                orderId,
                status = status.ToString(),
                note,
                occurredAt
            });

    public async Task NotifyDriverLocationAsync(int orderId, double latitude, double longitude, DateTime recordedAt, int? etaMinutes)
        => await _hubContext.Clients
            .Group(TrackingHub.GroupName(orderId))
            .SendAsync("driverLocationChanged", new
            {
                orderId,
                latitude,
                longitude,
                recordedAt,
                etaMinutes
            });
}
