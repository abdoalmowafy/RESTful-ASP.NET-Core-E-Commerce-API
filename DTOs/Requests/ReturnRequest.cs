using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs.Requests
{
    public class ReturnRequest
    {
        [Required] public required int OrderId { get; set; }
        [Required] public required int OrderProductId { get; set; }
        [Required] public required bool DeliveryNeeded { get; set; }
        [Required] public required int AddressId { get; set; }
        [Required] public required string ReturnReason { get; set; }
        [Required] public required int QuantityToReturn { get; set; }
    }
}
