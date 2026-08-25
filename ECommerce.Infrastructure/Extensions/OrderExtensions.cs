using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;

namespace ECommerce.Infrastructure.Extensions;

public static class OrderExtensions
{
    public static void RecordStatus(this Order order, string? note = null)
        => order.StatusEvents.Add(new OrderStatusEvent
        {
            Status = order.Status,
            Note = note
        });
}
