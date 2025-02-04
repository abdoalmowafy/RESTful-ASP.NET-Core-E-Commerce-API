using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class OrderProduct
    {
        [Key] public int Id { get; set; }
        [Required] public required Product Product { get; set; }
        [Required][Range(0, long.MaxValue)] public long ProductPriceCents { get; set; }
        [Required][Range(0, 99)] public float SalePercent { get; set; }
        [Required][Range(0, int.MaxValue)] public int Quantity { get; set; }
        [Required][Range(14, int.MaxValue)] public int WarrantyDays { get; set; }
        [DataType(DataType.DateTime)] public DateTime? PartiallyOrFullyReturnedDateTime { get; set; }
    }
}
