using ECommerceAPI.Models;

namespace ECommerceAPI.DTOs.Responses
{
    public class ProductDetailedResponse
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? SKU { get; set; }
        public string? Description { get; set; }
        public CategoryResponse? Category { get; set; }
        public int? Quantity { get; set; }
        public long? PriceCents { get; set; }
        public int? SalePercent { get; set; }
        public int? WarrantyDays { get; set; }
        public ICollection<ReviewResponse>? Reviews { get; set; }

    }
}
