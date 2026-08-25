using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class PromoCode : IHasEditHistory, IHasConcurrencyToken
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Percent { get; set; }
    public long? MaxSaleCents { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public uint RowVersion { get; set; }
    public ICollection<Cart> Carts { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<EditHistory> EditsHistory { get; set; } = [];
}
