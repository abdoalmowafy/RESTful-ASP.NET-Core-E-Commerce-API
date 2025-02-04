using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs.Requests
{
    public class AddressRequest
    {
        [Required] public required string Apartment { get; set; }
        [Required] public required string Floor { get; set; }
        [Required] public required string Building { get; set; }
        [Required] public required string Street { get; set; }
        [Required] public required string City { get; set; }
        [Required] public required string State { get; set; }
        [Required] public required string Country { get; set; }
        [Required][DataType(DataType.PostalCode)] public required string PostalCode { get; set; }
    }
}
