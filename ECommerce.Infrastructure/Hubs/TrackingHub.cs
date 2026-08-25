using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.Infrastructure.Hubs;

public class TrackingHub(AppDbContext context) : Hub
{
    public const string HubPath = "/hubs/tracking";

    public static string GroupName(int orderId) => $"order-{orderId}";

    public override Task OnConnectedAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            Context.Abort();

        return base.OnConnectedAsync();
    }

    [Authorize]
    public async Task JoinOrder(int orderId)
    {
        var userId = Context.User?.GetUserId();
        if (userId is null)
            return;

        var isStaff = Context.User!.IsStaff();
        var owns = await context.Orders
            .AsNoTracking()
            .AnyAsync(o => o.Id == orderId && o.UserId == userId);

        if (isStaff || owns)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(orderId));
    }

    public async Task LeaveOrder(int orderId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(orderId));
}
