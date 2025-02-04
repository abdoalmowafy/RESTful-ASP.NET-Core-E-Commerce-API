using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class Product
    {
        [Key] public int Id { get; set; }
        [Required] public required string Name { get; set; }
        [Required] public required string SKU { get; set; }
        [Required][DataType(DataType.MultilineText)] public required string Description { get; set; }
        [Required] public required int CategoryId { get; set; }
        [Required][ForeignKey("CategoryId")] public Category? Category { get; set; }
        [Required][Range(0, int.MaxValue)] public required int Quantity { get; set; }
        public long Views { get; set; } = 0;
        [Required][Range(0, long.MaxValue)] public required long PriceCents { get; set; }
        [Required][Range(0, 99)] public int SalePercent { get; set; } = 0;
        public ICollection<Review> Reviews { get; set; } = [];
        [Required][Range(14, int.MaxValue)] public int WarrantyDays { get; set; } = 14;
        [Required][DataType(DataType.DateTime)] public DateTime CreatedDateTime { get; set; } = DateTime.Now;
        [DataType(DataType.DateTime)] public DateTime? DeletedDateTime { get; set; }
        public ICollection<EditHistory> EditsHistory { get; set; } = [];

        [FileExtensions(Extensions = "jpg,jpeg,png,gif,mp4,mov,avi,webm")]
        [Required][NotMapped][DataType(DataType.Upload)] public IList<IFormFile> Media { get; set; } = [];
    }
}