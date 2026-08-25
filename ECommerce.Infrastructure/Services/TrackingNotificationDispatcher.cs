using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Hubs;

using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Composite notifier: SignalR live feed (in-app) + FCM push to the order owner's devices.
/// Dead tokens reported by FCM are removed from the registry.
/// </summary>
public class TrackingNotificationDispatcher(
    AppDbContext context,
    IPushSender pushSender,
    IDeviceTokenService deviceTokenService,
    IHubContext<TrackingHub> hubContext) : IOrderTrackingNotifier
{
    private readonly AppDbContext _context = context;
    private readonly IPushSender _pushSender = pushSender;
    private readonly IDeviceTokenService _deviceTokenService = deviceTokenService;
    private readonly SignalROrderTrackingNotifier _signalR = new(hubContext);

    public async Task NotifyStatusAsync(int orderId, OrderStatus status, string? note, DateTime occurredAt)
    {
        await _signalR.NotifyStatusAsync(orderId, status, note, occurredAt);

        var order = await _context.Orders
            .AsNoTracking()
            .Select(o => new { o.Id, o.UserId })
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return;

        var tokens = await _context.DeviceTokens
            .AsNoTracking()
            .Where(t => t.OwnerId == order.UserId)
            .Select(t => t.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        var title = $"Order #{orderId} update";
        var body = $"Your order is now {status}.{(string.IsNullOrEmpty(note) ? string.Empty : $" {note}")}";
        var data = new Dictionary<string, string> { ["orderId"] = orderId.ToString(), ["type"] = "order-status" };

        var dead = await _pushSender.SendToTokensAsync(tokens, title, body, data);
        if (dead.Count > 0)
            await _deviceTokenService.RemoveDeadTokensAsync(dead);
    }

    // High-frequency by design: live map feed stays SignalR-only.
    public Task NotifyDriverLocationAsync(int orderId, double latitude, double longitude, DateTime recordedAt, int? etaMinutes)
        => Task.CompletedTask;
}
