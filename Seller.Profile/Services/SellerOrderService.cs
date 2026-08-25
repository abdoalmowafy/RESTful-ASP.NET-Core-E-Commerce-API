using Seller.Profile.Contracts;

namespace Seller.Profile.Services;

public interface ISellerOrderService
{
    Task<Result<PaginatedList<SellerOrderItemResponse>>> GetSoldItemsAsync(string ownerId, OrderStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}

public class SellerOrderService(AppDbContext context) : ISellerOrderService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<PaginatedList<SellerOrderItemResponse>>> GetSoldItemsAsync(
        string ownerId,
        OrderStatus? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.OrderProducts
            .AsNoTracking()
            .Include(op => op.Product)
            .Include(op => op.Order)
            .Where(op =>
                op.Product!.Store!.OwnerId == ownerId &&
                op.Order!.DeletedAt == null &&
                op.Order.Status != OrderStatus.Paying);

        if (status.HasValue)
            query = query.Where(op => op.Order!.Status == status.Value);

        var ordered = query
            .OrderByDescending(op => op.Order!.CreatedAt)
            .ThenBy(op => op.Id);

        var count = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(op => new SellerOrderItemResponse(
            op.Id,
            op.OrderId,
            op.Order!.Status,
            op.Product?.Name ?? string.Empty,
            op.Quantity,
            op.ProductPriceCents * (100 - op.SalePercent) / 100 * op.Quantity,
            op.Order.CreatedAt)).ToList();

        return Result.Succeed(new PaginatedList<SellerOrderItemResponse>(mapped, pageIndex, count, pageSize));
    }
}
