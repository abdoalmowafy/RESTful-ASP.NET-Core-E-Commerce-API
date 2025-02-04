namespace ECommerceAPI.DTOs.Responses
{
    public class OrderProductResponse
    {
        public int? Id { get; set; }
        public ProductPriefResponse? Product { get; set; }
        public long? ProductPriceCents { get; set; }
        public float? SalePercent { get; set; }
        public int? Quantity { get; set; }
        public TimeSpan? Warranty { get; set; }
    }
}
