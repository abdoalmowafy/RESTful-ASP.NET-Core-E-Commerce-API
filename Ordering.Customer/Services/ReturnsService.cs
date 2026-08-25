using Ordering.Customer.Contracts;

namespace Ordering.Customer.Services;

public interface IReturnsService
{
    Task<Result<PaginatedList<ReturnRequestResponse>>> GetMyReturnsAsync(string userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<ReturnRequestResponse>> CreateAsync(string userId, int orderProductId, CreateReturnRequest request, CancellationToken cancellationToken = default);
}

public class ReturnsService(AppDbContext context) : IReturnsService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<PaginatedList<ReturnRequestResponse>>> GetMyReturnsAsync(string userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.OrderProduct).ThenInclude(op => op!.Product)
            .Where(r => r.RequestedById == userId && r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt);

        var page = await PaginatedList<ReturnRequest>.CreateAsync(query, pageIndex, pageSize, cancellationToken);
        var mapped = page.Items.Select(Map).ToList();

        return Result.Succeed(new PaginatedList<ReturnRequestResponse>(mapped, page.PageNumber, page.TotalCount, page.TotalPages));
    }

    public async Task<Result<ReturnRequestResponse>> CreateAsync(string userId, int orderProductId, CreateReturnRequest request, CancellationToken cancellationToken = default)
    {
        var orderProduct = await _context.OrderProducts
            .Include(op => op.Product)
            .Include(op => op.Order!)
                .ThenInclude(o => o.Address)
            .FirstOrDefaultAsync(op =>
                op.Id == orderProductId &&
                op.Order!.UserId == userId &&
                op.Order.Status == OrderStatus.Delivered &&
                op.Order.DeletedAt == null,
                cancellationToken);

        if (orderProduct is null)
            return Result.Failure<ReturnRequestResponse>(OrderingErrors.Return.OrderNotDelivered);

        if (orderProduct.ReturnedAt is not null)
            return Result.Failure<ReturnRequestResponse>(OrderingErrors.Return.AlreadyRequested);

        if (await _context.ReturnRequests.AnyAsync(
                rr => rr.OrderProductId == orderProductId && rr.Status != ReturnStatus.Cancelled && rr.DeletedAt == null,
                cancellationToken))
            return Result.Failure<ReturnRequestResponse>(OrderingErrors.Return.AlreadyRequested);

        if (request.Quantity > orderProduct.Quantity)
            return Result.Failure<ReturnRequestResponse>(OrderingErrors.Return.ExceedsQuantity);

        if (orderProduct.Order!.DeliveredAt.HasValue &&
            DateTime.UtcNow > orderProduct.Order.DeliveredAt.Value.AddDays(orderProduct.WarrantyDays))
            return Result.Failure<ReturnRequestResponse>(OrderingErrors.Return.WarrantyExpired);

        var pickupAddress = await _context.Addresses.FirstOrDefaultAsync(
            a => a.Id == request.AddressId && a.UserId == userId && a.DeletedAt == null,
            cancellationToken);

        if (pickupAddress is null)
            return Result.Failure<ReturnRequestResponse>(OrderingErrors.Return.OrderNotDelivered);

        var returnRequest = new ReturnRequest
        {
            OrderId = orderProduct.OrderId,
            OrderProductId = orderProductId,
            RequestedById = userId,
            AddressId = pickupAddress.Id,
            Reason = request.Reason,
            Quantity = request.Quantity,
            Status = ReturnStatus.Processing
        };

        _context.ReturnRequests.Add(returnRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(Map(returnRequest));
    }

    private static ReturnRequestResponse Map(ReturnRequest r)
        => new(
            r.Id,
            r.OrderId,
            r.OrderProductId,
            r.OrderProduct?.Product?.Name ?? string.Empty,
            r.Quantity,
            r.Reason,
            r.Status,
            r.CreatedAt);
}
