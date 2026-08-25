namespace ECommerce.Infrastructure.Entities;

public class Cart : IHasConcurrencyToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int? PromoCodeId { get; set; }
    public PromoCode? PromoCode { get; set; }
    public uint RowVersion { get; set; }
    public ICollection<CartProduct> CartProducts { get; set; } = [];
}
