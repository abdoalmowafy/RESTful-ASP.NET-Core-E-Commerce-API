namespace ECommerceAPI.DTOs.Responses
{
    public class CartProductResponse
    {
        public ProductDetailedResponse? Product { get; set; }
        public int? Quantity { get; set; }
    }
}
