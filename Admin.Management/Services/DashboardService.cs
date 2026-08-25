using Admin.Management.Contracts;

namespace Admin.Management.Services;

public interface IDashboardService
{
    Task<Result<DashboardResponse>> GetAsync(CancellationToken cancellationToken = default);
}

public class DashboardService(AppDbContext context) : IDashboardService
{
    private static readonly OrderStatus[] RevenueStatuses =
        [OrderStatus.Processing, OrderStatus.OnTheWay, OrderStatus.Delivered];

    private readonly AppDbContext _context = context;

    public async Task<Result<DashboardResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthAgo = todayStart.AddDays(-30);
        var fortnightAgo = todayStart.AddDays(-13);

        var revenueCents = await _context.Orders
            .Where(o => o.DeletedAt == null && RevenueStatuses.Contains(o.Status))
            .SumAsync(o => o.TotalCents, cancellationToken);

        var revenueLast30Days = await _context.Orders
            .Where(o => o.DeletedAt == null &&
                        o.CreatedAt >= monthAgo &&
                        RevenueStatuses.Contains(o.Status))
            .SumAsync(o => o.TotalCents, cancellationToken);

        var activeOrders = await _context.Orders
            .CountAsync(o => o.DeletedAt == null && RevenueStatuses.Contains(o.Status), cancellationToken);

        var customersCount = await _context.Users
            .CountAsync(cancellationToken);

        var pendingReturns = await _context.ReturnRequests
            .CountAsync(r => r.DeletedAt == null && r.Status != ReturnStatus.Returned && r.Status != ReturnStatus.Cancelled, cancellationToken);

        var recentOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.DeletedAt == null && o.CreatedAt >= fortnightAgo)
            .Select(o => new { o.CreatedAt })
            .ToListAsync(cancellationToken);

        var last14Days = Enumerable
            .Range(0, 14)
            .Select(i =>
            {
                var day = todayStart.AddDays(-13 + i);
                return new DailyOrdersPoint(
                    day.ToString("yyyy-MM-dd"),
                    recentOrders.Count(o => o.CreatedAt.Date == day));
            })
            .ToList();

        var lowStock = await _context.Products
            .AsNoTracking()
            .Where(p => p.DeletedAt == null && p.Quantity <= 5)
            .OrderBy(p => p.Quantity)
            .Take(10)
            .Select(p => new LowStockItem(p.Id, p.Name, p.Sku, p.Quantity))
            .ToListAsync(cancellationToken);

        return Result.Succeed(new DashboardResponse(
            revenueCents,
            revenueLast30Days,
            activeOrders,
            customersCount,
            pendingReturns,
            last14Days,
            lowStock));
    }
}
