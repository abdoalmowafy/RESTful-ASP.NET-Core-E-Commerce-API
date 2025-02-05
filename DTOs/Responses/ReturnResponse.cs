using ECommerceAPI.Models;

namespace ECommerceAPI.DTOs.Responses
{
    public class ReturnResponse
    {
        public int? Id { get; set; }
        public UserDetailedResponse? Transporter { get; set; }
        public AddressResponse? Address { get; set; }
        public OrderResponse? Order { get; set; }
        public OrderProductResponse? OrderProduct { get; set; }
        public string? ReturnReason { get; set; }
        public int? Quantity { get; set; }
        public ReturnStatus? Status { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public DateTime? ReturnedDateTime { get; set; }
        public DateTime? DeletedDateTime { get; set; }
    }
}
