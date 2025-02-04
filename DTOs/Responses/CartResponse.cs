namespace ECommerceAPI.DTOs.Responses
{
    public class CartResponse
    {
        public ICollection<CartProductResponse> CartProducts { get; set; } = [];
        public PromoCodeResponse? PromoCode { get; set; }
    }
}
