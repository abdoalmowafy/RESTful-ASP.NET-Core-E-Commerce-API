using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class Store
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public StoreStatus Status { get; set; } = StoreStatus.PendingVerification;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Offer> Offers { get; set; } = [];
}
