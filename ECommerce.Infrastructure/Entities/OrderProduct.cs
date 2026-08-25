namespace ECommerce.Infrastructure.Entities;

public class OrderProduct
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public long ProductPriceCents { get; set; }
    public int SalePercent { get; set; }
    public int Quantity { get; set; }
    public int WarrantyDays { get; set; }
    public DateTime? ReturnedAt { get; set; }
}
