using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class CartProduct
    {
        [Key] public int Id { get; set; }
        [Required] public required Product Product { get; set; }
        [Required][Range(0,int.MaxValue)] public required int Quantity { get; set; }
    }
}
