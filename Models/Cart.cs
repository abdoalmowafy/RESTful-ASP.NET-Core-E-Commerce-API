using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class Cart
    {
        [Key] public int Id { get; set; }
        [Required] public ICollection<CartProduct> CartProducts { get; set; } = [];
        public PromoCode? PromoCode { get; set; }
    }
}
