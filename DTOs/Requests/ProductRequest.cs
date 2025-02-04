using ECommerceAPI.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs.Requests
{
    public class ProductRequest
    {
        [Required] public required string Name { get; set; }
        [Required] public required string SKU { get; set; }
        [Required][DataType(DataType.MultilineText)] public required string Description { get; set; }
        [Required][ForeignKey("CategoryId")] public required Category Category { get; set; }
        [Required][Range(0, int.MaxValue)] public required int Quantity { get; set; }
        [Required][Range(0, long.MaxValue)] public required long PriceCents { get; set; }
        [Required][Range(0, 99)] public int SalePercent { get; set; } = 0;
        [Required][Range(14, int.MaxValue)] public int WarrantyDays { get; set; } = 14;

        [FileExtensions(Extensions = "jpg,jpeg,png,gif,mp4,mov,avi,webm")]
        [Required][NotMapped][DataType(DataType.Upload)] public IList<IFormFile> Media { get; set; } = [];
    }
}
