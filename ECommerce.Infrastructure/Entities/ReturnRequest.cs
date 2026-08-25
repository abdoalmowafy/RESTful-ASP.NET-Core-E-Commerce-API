using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class ReturnRequest : IHasConcurrencyToken
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int OrderProductId { get; set; }
    public OrderProduct? OrderProduct { get; set; }
    public string RequestedById { get; set; } = string.Empty;
    public ApplicationUser? RequestedBy { get; set; }
    public string? TransporterId { get; set; }
    public ApplicationUser? Transporter { get; set; }
    public int AddressId { get; set; }
    public Address? Address { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public ReturnStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }
    public uint RowVersion { get; set; }
    public DateTime? DeletedAt { get; set; }
}
