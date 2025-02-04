using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAPI.Models
{
    public class Address
    {
        [Key] public int Id { get; set; }
        [Required] public required string Apartment { get; set; }
        [Required] public required string Floor { get; set; }
        [Required] public required string Building { get; set; }
        [Required] public required string Street { get; set; }
        [Required] public required string City { get; set; }
        [Required] public required string State { get; set; }
        [Required] public required string Country { get; set; }
        [Required][DataType(DataType.PostalCode)] public required string PostalCode { get; set; }
        [Required][DataType(DataType.DateTime)] public DateTime CreatedDateTime { get; set; } = DateTime.Now;
        [DataType(DataType.DateTime)] public DateTime? DeletedDateTime { get; set; }
        public ICollection<EditHistory> EditsHistory { get; set; } = [];
    }
}
