namespace ECommerce.Infrastructure.Entities;

public class CartProduct : IHasConcurrencyToken
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart? Cart { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public uint RowVersion { get; set; }
    public int Quantity { get; set; } = 1;
}
