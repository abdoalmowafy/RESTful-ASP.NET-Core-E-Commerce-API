using Driver.Profile.Contracts;

namespace Driver.Profile.Services;

public interface IDeliveryService
{
    Task<Result<IReadOnlyList<DeliveryResponse>>> GetMyDeliveriesAsync(string driverId, CancellationToken cancellationToken = default);
    Task<Result> MarkPickedUpAsync(string driverId, int orderId, CancellationToken cancellationToken = default);
    Task<Result> MarkDeliveredAsync(string driverId, int orderId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PickupResponse>>> GetMyPickupsAsync(string driverId, CancellationToken cancellationToken = default);
    Task<Result> MarkCollectedAsync(string driverId, int returnId, CancellationToken cancellationToken = default);
    Task<Result> MarkCompletedAsync(string driverId, int returnId, CancellationToken cancellationToken = default);
}

public class DeliveryService(AppDbContext context, IOrderTrackingNotifier trackingNotifier) : IDeliveryService
{
    private readonly AppDbContext _context = context;
    private readonly IOrderTrackingNotifier _trackingNotifier = trackingNotifier;

    public async Task<Result<IReadOnlyList<DeliveryResponse>>> GetMyDeliveriesAsync(string driverId, CancellationToken cancellationToken = default)
    {
        var deliveries = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Address)
            .Include(o => o.OrderProducts)
            .Where(o =>
                o.DeletedAt == null &&
                o.DeliveryNeeded &&
                o.TransporterId == driverId &&
                (o.Status == OrderStatus.Processing || o.Status == OrderStatus.OnTheWay))
            .OrderBy(o => o.CreatedAt)
            .Select(MapDelivery())
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<DeliveryResponse>>(deliveries);
    }

    public async Task<Result> MarkPickedUpAsync(string driverId, int orderId, CancellationToken cancellationToken = default)
        => await TransitionOrderAsync(driverId, orderId, OrderStatus.Processing, OrderStatus.OnTheWay, cancellationToken: cancellationToken);

    public async Task<Result> MarkDeliveredAsync(string driverId, int orderId, CancellationToken cancellationToken = default)
        => await TransitionOrderAsync(driverId, orderId, OrderStatus.OnTheWay, OrderStatus.Delivered, markDeliveredAt: true, cancellationToken: cancellationToken);

    public async Task<Result<IReadOnlyList<PickupResponse>>> GetMyPickupsAsync(string driverId, CancellationToken cancellationToken = default)
    {
        var pickups = await _context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.RequestedBy)
            .Include(r => r.Address)
            .Include(r => r.OrderProduct).ThenInclude(op => op!.Product)
            .Where(r =>
                r.DeletedAt == null &&
                r.TransporterId == driverId &&
                (r.Status == ReturnStatus.Processing || r.Status == ReturnStatus.OnTheWay))
            .OrderBy(r => r.CreatedAt)
            .Select(MapPickup())
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<PickupResponse>>(pickups);
    }

    public async Task<Result> MarkCollectedAsync(string driverId, int returnId, CancellationToken cancellationToken = default)
        => await TransitionReturnAsync(driverId, returnId, ReturnStatus.Processing, ReturnStatus.OnTheWay, cancellationToken);

    public async Task<Result> MarkCompletedAsync(string driverId, int returnId, CancellationToken cancellationToken = default)
        => await TransitionReturnAsync(driverId, returnId, ReturnStatus.OnTheWay, ReturnStatus.Returned, cancellationToken);

    private async Task<Result> TransitionOrderAsync(
        string driverId,
        int orderId,
        OrderStatus from,
        OrderStatus to,
        bool markDeliveredAt = false,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.DeletedAt == null, cancellationToken);

        if (order is null || order.TransporterId != driverId || !order.DeliveryNeeded)
            return Result.Failure(OrderingErrors.Order.NotFound);

        if (order.Status != from)
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        order.Status = to;

        if (markDeliveredAt)
            order.DeliveredAt = DateTime.UtcNow;

        order.RecordStatus("Delivery updated by driver");
        await _context.SaveChangesAsync(cancellationToken);
        await _trackingNotifier.NotifyStatusAsync(order.Id, order.Status, "Delivery updated by driver", DateTime.UtcNow);
        return Result.Succeed();
    }

    private async Task<Result> TransitionReturnAsync(
        string driverId,
        int returnId,
        ReturnStatus from,
        ReturnStatus to,
        CancellationToken cancellationToken = default)
    {
        var returnRequest = await _context.ReturnRequests
            .Include(r => r.OrderProduct)
            .FirstOrDefaultAsync(r => r.Id == returnId && r.DeletedAt == null, cancellationToken);

        if (returnRequest is null || returnRequest.TransporterId != driverId)
            return Result.Failure(OrderingErrors.Return.NotFound);

        if (returnRequest.Status != from)
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        returnRequest.Status = to;

        if (to == ReturnStatus.Returned)
        {
            returnRequest.ReturnedAt = DateTime.UtcNow;
            if (returnRequest.OrderProduct is not null)
                returnRequest.OrderProduct.ReturnedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Succeed();
    }

    private static System.Linq.Expressions.Expression<Func<Order, DeliveryResponse>> MapDelivery()
        => o => new DeliveryResponse(
            o.Id,
            $"{o.User!.FirstName} {o.User.LastName}".Trim(),
            o.Address!.City,
            $"{o.Address.Building}, {o.Address.Street}, {o.Address.Apartment}",
            o.TotalCents,
            o.OrderProducts.Sum(op => op.Quantity),
            o.CreatedAt);

    private static System.Linq.Expressions.Expression<Func<ReturnRequest, PickupResponse>> MapPickup()
        => r => new PickupResponse(
            r.Id,
            $"{r.RequestedBy!.FirstName} {r.RequestedBy.LastName}".Trim(),
            r.Address!.City,
            $"{r.Address.Building}, {r.Address.Street}, {r.Address.Apartment}",
            r.OrderProduct!.Product!.Name,
            r.Quantity,
            r.CreatedAt);
}
