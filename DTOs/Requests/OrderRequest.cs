using ECommerceAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs.Requests
{
    public class OrderRequest
    {
        [Required] public required PaymentMethod PaymentMethod { get; set; }
        [Required] public required bool DeliveryNeeded { get; set; }
        [Required] public required int AddressId { get; set; }
        public string? Identifier { get; set; }
    }
}
