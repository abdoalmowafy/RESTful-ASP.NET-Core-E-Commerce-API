using ECommerceAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs.Responses
{
    public class OrderResponse
    {
        public int? Id { get; set; }
        public UserDetailedResponse? User { get; set; }
        public UserDetailedResponse? Transporter { get; set; }
        public ICollection<OrderProductResponse>? OrderProducts { get; set; }
        public PromoCodeResponse? PromoCode { get; set; }
        public long? TotalCents { get; set; }
        public string? Currency { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public bool? DeliveryNeeded { get; set; }
        public OrderStatus? Status { get; set; }
        public int? PaymobOrderId { get; set; }
        public required AddressResponse? Address { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public DateTime? DeliveryDateTime { get; set; }
        public DateTime? DeletedDateTime { get; set; }
    }
}
