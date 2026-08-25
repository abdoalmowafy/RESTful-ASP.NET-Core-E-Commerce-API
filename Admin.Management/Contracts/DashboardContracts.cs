namespace Admin.Management.Contracts;

public record DailyOrdersPoint(string Date, int Orders);

public record LowStockItem(int ProductId, string Name, string Sku, int Quantity);

public record DashboardResponse(
    long RevenueCents,
    long RevenueLast30DaysCents,
    int ActiveOrders,
    int CustomersCount,
    int PendingReturns,
    IReadOnlyList<DailyOrdersPoint> Last14Days,
    IReadOnlyList<LowStockItem> LowStock);
