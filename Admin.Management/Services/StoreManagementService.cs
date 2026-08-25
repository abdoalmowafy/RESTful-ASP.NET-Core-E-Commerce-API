using Admin.Management.Contracts;

namespace Admin.Management.Services;

public interface IStoreManagementService
{
    Task<Result<PaginatedList<StoreManagementResponse>>> GetAsync(StoreStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusAsync(int storeId, UpdateStoreStatusRequest request, CancellationToken cancellationToken = default);
}

public class StoreManagementService(AppDbContext context) : IStoreManagementService
{
    private static readonly Dictionary<StoreStatus, StoreStatus[]> AllowedTransitions = new()
    {
        [StoreStatus.Active] = [StoreStatus.PendingVerification, StoreStatus.Suspended],
        [StoreStatus.Rejected] = [StoreStatus.PendingVerification],
        [StoreStatus.Suspended] = [StoreStatus.Active]
    };

    private readonly AppDbContext _context = context;

    public async Task<Result<PaginatedList<StoreManagementResponse>>> GetAsync(
        StoreStatus? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Stores
            .AsNoTracking()
            .Include(s => s.Owner)
            .OrderByDescending(s => s.CreatedAt);

        if (status.HasValue)
            query = (IOrderedQueryable<Store>)query.Where(s => s.Status == status.Value);

        var page = await PaginatedList<Store>.CreateAsync(query, pageIndex, pageSize, cancellationToken);
        var storeIds = page.Items.Select(s => s.Id).ToList();

        var productCounts = await _context.Products
            .Where(p => storeIds.Contains(p.StoreId))
            .GroupBy(p => p.StoreId)
            .Select(g => new { StoreId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StoreId, x => x.Count, cancellationToken);

        var mapped = page.Items.Select(s => Map(s, productCounts.GetValueOrDefault(s.Id))).ToList();

        return Result.Succeed(new PaginatedList<StoreManagementResponse>(mapped, page.PageNumber, page.TotalCount, pageSize));
    }

    public async Task<Result> UpdateStatusAsync(int storeId, UpdateStoreStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Status == StoreStatus.PendingVerification)
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.DeletedAt == null, cancellationToken);
        if (store is null)
            return Result.Failure(MarketplaceErrors.Store.NotFound);

        if (store.Status == request.Status)
            return Result.Succeed();

        if (!AllowedTransitions.TryGetValue(request.Status, out var allowedFrom) || !allowedFrom.Contains(store.Status))
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        if (request.Status == StoreStatus.Rejected && string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(Error.BadRequest("Store.RejectionReasonRequired", "A rejection reason is required"));

        store.Status = request.Status;
        store.RejectionReason = request.Status == StoreStatus.Rejected ? request.Reason : null;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Succeed();
    }

    private static StoreManagementResponse Map(Store s, int productsCount)
        => new(
            s.Id,
            s.Name,
            s.Slug,
            s.Description,
            $"{s.Owner?.FirstName} {s.Owner?.LastName}".Trim(),
            s.Owner?.Email ?? string.Empty,
            s.Status,
            s.RejectionReason,
            productsCount,
            s.CreatedAt);
}
