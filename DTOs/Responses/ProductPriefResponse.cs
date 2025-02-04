using ECommerceAPI.Models;

namespace ECommerceAPI.DTOs.Responses
{
    public class ProductPriefResponse
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public CategoryResponse? Category { get; set; }
        public long? PriceCents { get; set; }
        public int? SalePercent { get; set; }

    }
}
