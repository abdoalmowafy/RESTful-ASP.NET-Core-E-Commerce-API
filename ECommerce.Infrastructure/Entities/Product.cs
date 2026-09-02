using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class Product : IHasEditHistory, IHasConcurrencyToken
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int StoreId { get; set; }
    public Store? Store { get; set; }
    public int Quantity { get; set; }
    public long Views { get; set; }
    public long PriceCents { get; set; }
    public int SalePercent { get; set; }
    public int WarrantyDays { get; set; } = 14;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public uint RowVersion { get; set; }
    public ICollection<ProductMedia> Media { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<ApplicationUser> WishlistedBy { get; set; } = [];
    public ICollection<EditHistory> EditsHistory { get; set; } = [];
    public ICollection<OfferProduct> Offers { get; set; } = [];

    public long FinalPriceCents => PriceCents * (100 - SalePercent) / 100;
}
