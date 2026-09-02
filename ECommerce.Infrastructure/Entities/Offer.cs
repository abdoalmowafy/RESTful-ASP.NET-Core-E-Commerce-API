namespace ECommerce.Infrastructure.Entities;

public class Offer
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public Store? Store { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DiscountPercent { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OfferProduct> Products { get; set; } = [];
}
