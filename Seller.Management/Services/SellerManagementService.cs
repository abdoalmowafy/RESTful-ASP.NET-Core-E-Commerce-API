using Seller.Management.Contracts;

namespace Seller.Management.Services;

public interface ISellerManagementService
{
    Task<Result<PaginatedList<SellerManagementResponse>>> GetAsync(StoreStatus? status, string? search, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}

public class SellerManagementService(AppDbContext context) : ISellerManagementService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<PaginatedList<SellerManagementResponse>>> GetAsync(
        StoreStatus? status,
        string? search,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Stores
            .AsNoTracking()
            .Include(s => s.Owner)
            .Where(s => s.DeletedAt == null);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.Name.Contains(search) ||
                s.Owner!.Email!.Contains(search));

        var page = await PaginatedList<Store>.CreateAsync(
            (IOrderedQueryable<Store>)query.OrderByDescending(s => s.CreatedAt),
            pageIndex, pageSize, cancellationToken);

        var storeIds = page.Items.Select(s => s.Id).ToList();
        var productCounts = await _context.Products
            .Where(p => storeIds.Contains(p.StoreId))
            .GroupBy(p => p.StoreId)
            .Select(g => new { StoreId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StoreId, x => x.Count, cancellationToken);

        var mapped = page.Items.Select(s => new SellerManagementResponse(
            s.Id,
            s.Name,
            s.OwnerId,
            $"{s.Owner?.FirstName} {s.Owner?.LastName}".Trim(),
            s.Owner?.Email ?? string.Empty,
            s.Slug,
            s.Status,
            productCounts.GetValueOrDefault(s.Id),
            s.CreatedAt)).ToList();

        return Result.Succeed(new PaginatedList<SellerManagementResponse>(mapped, page.PageNumber, page.TotalCount, pageSize));
    }
}
