using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class Order : IHasConcurrencyToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public string? TransporterId { get; set; }
    public ApplicationUser? Transporter { get; set; }
    public int? PromoCodeId { get; set; }
    public PromoCode? PromoCode { get; set; }
    public long TotalCents { get; set; }
    public string Currency { get; set; } = "EGP";
    public PaymentMethod PaymentMethod { get; set; }
    public bool DeliveryNeeded { get; set; }
    public OrderStatus Status { get; set; }
    public int? PaymobOrderId { get; set; }
    public int AddressId { get; set; }
    public Address? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public uint RowVersion { get; set; }
    public ICollection<OrderProduct> OrderProducts { get; set; } = [];
    public ICollection<OrderStatusEvent> StatusEvents { get; set; } = [];
}
