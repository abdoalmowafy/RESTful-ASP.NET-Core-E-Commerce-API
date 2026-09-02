namespace ECommerce.Infrastructure.Entities;

public class OfferProduct
{
    public int OfferId { get; set; }
    public Offer? Offer { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
}
