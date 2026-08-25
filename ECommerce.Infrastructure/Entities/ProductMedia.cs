namespace ECommerce.Infrastructure.Entities;

public class ProductMedia
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Url { get; set; } = string.Empty;
}
