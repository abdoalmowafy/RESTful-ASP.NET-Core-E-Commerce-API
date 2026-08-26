using Ordering.Management.Contracts;

namespace Ordering.Management.Services;

public interface IOrderManagementService
{
    Task<Result<PaginatedList<ManagementOrderResponse>>> GetAsync(OrderStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result> AssignTransporterAsync(int orderId, AssignTransporterRequest request, CancellationToken cancellationToken = default);
}

public class OrderManagementService(AppDbContext context, IOrderTrackingNotifier trackingNotifier) : IOrderManagementService
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.OnTheWay] = [OrderStatus.Processing],
        [OrderStatus.Delivered] = [OrderStatus.OnTheWay],
        [OrderStatus.Cancelled] = [OrderStatus.Processing]
    };

    private readonly AppDbContext _context = context;
    private readonly IOrderTrackingNotifier _trackingNotifier = trackingNotifier;

    public async Task<Result<PaginatedList<ManagementOrderResponse>>> GetAsync(OrderStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Transporter)
            .Include(o => o.Address)
            .Include(o => o.OrderProducts)
            .Where(o => o.DeletedAt == null)
            .OrderByDescending(o => o.CreatedAt);

        if (status.HasValue)
            query = (IOrderedQueryable<Order>)query.Where(o => o.Status == status.Value);

        var page = await PaginatedList<Order>.CreateAsync(query, pageIndex, pageSize, cancellationToken);
        var mapped = page.Items.Select(Map).ToList();

        return Result.Succeed(new PaginatedList<ManagementOrderResponse>(mapped, page.PageNumber, page.TotalCount, page.TotalPages));
    }

    public async Task<Result> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.DeletedAt == null, cancellationToken);
        if (order is null)
            return Result.Failure(OrderingErrors.Order.NotFound);

        if (order.Status == request.Status)
            return Result.Succeed();

        if (!AllowedTransitions.TryGetValue(request.Status, out var allowedFrom) || !allowedFrom.Contains(order.Status))
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        order.Status = request.Status;

        if (request.Status == OrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow;

        order.RecordStatus("Updated by staff");
        await _context.SaveChangesAsync(cancellationToken);
        await _trackingNotifier.NotifyStatusAsync(order.Id, order.Status, "Updated by staff", DateTime.UtcNow);
        return Result.Succeed();
    }

    public async Task<Result> AssignTransporterAsync(int orderId, AssignTransporterRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.DeletedAt == null, cancellationToken);
        if (order is null)
            return Result.Failure(OrderingErrors.Order.NotFound);

        var transporter = await _context.Users.FindAsync([request.TransporterId], cancellationToken);
        if (transporter is null)
            return Result.Failure(UserErrors.NotFound);

        if (!await _context.UserRoles
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, RoleName = r.Name })
                .AnyAsync(x => x.UserId == transporter.Id && x.RoleName == "Driver", cancellationToken))
            return Result.Failure(OrderingErrors.Order.TransporterRoleRequired);

        order.TransporterId = transporter.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed();
    }

    private static ManagementOrderResponse Map(Order o)
        => new(
            o.Id,
            $"{o.User?.FirstName} {o.User?.LastName}".Trim(),
            o.Transporter is null ? null : $"{o.Transporter.FirstName} {o.Transporter.LastName}".Trim(),
            o.TotalCents,
            o.PaymentMethod,
            o.DeliveryNeeded,
            o.Status,
            o.CreatedAt,
            o.OrderProducts.Sum(op => op.Quantity),
            o.Address?.City ?? string.Empty);
}
