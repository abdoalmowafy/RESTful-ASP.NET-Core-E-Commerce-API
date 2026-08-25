using ECommerce.Infrastructure.Abstractions;

namespace ECommerce.Infrastructure.Entities;

public class Address : IHasEditHistory
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Apartment { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsStoreAddress => UserId is null;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<EditHistory> EditsHistory { get; set; } = [];
}
