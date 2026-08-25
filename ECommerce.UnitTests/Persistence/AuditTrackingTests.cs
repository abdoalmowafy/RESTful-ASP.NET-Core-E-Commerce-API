using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Extensions;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Persistence;

public class AuditTrackingTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();

    [Fact]
    public async Task SaveChanges_records_field_level_edit_history()
    {
        var context = _sp.GetRequiredService<AppDbContext>();
        var category = TestData.Category();
        var product = TestData.Product(name: "Original", sku: "AUD-0001");
        product.Category = category;
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var tracked = await context.Products.FirstAsync(p => p.Sku == "AUD-0001");
        tracked.Name = "Renamed";
        tracked.PriceCents = 123456;
        await context.SaveChangesAsync();

        var edits = await context.EditHistories
            .Where(e => e.EntityType == nameof(Product) && e.EntityId == tracked.Id.ToString())
            .ToListAsync();

        Assert.Contains(edits, e => e.Field == nameof(Product.Name) && e.OldValue == "Original" && e.NewValue == "Renamed");
        Assert.Contains(edits, e => e.Field == nameof(Product.PriceCents) && e.OldValue == "10000" && e.NewValue == "123456");
        Assert.Equal(2, edits.Count);
    }

    [Fact]
    public async Task RecordStatus_appends_a_timeline_event()
    {
        var context = _sp.GetRequiredService<AppDbContext>();
        var order = new Order
        {
            UserId = Guid.NewGuid().ToString(),
            Status = OrderStatus.Processing,
            AddressId = 1
        };

        order.RecordStatus("Order placed");
        order.Status = OrderStatus.OnTheWay;
        order.RecordStatus("Picked up");

        Assert.Equal(2, order.StatusEvents.Count);
        Assert.Equal(OrderStatus.Processing, order.StatusEvents.First().Status);
        Assert.Equal("Order placed", order.StatusEvents.First().Note);
        Assert.Equal(OrderStatus.OnTheWay, order.StatusEvents.Last().Status);
        Assert.Equal("Picked up", order.StatusEvents.Last().Note);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
