using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ECommerceAPI.Models
{
    public class User : IdentityUser
    {
        [PersonalData][Required] public string? Name { get; set; }
        [PersonalData] public DateOnly? DOB { get; set; }
        [PersonalData] public Gender? Gender { get; set; }
        [PersonalData] public ICollection<Address> Addresses { get; set; } = [];
        public ICollection<Product> WishList { get; set; } = [];
        public ICollection<Order> Orders { get; set; } = [];
        public ICollection<ReturnProductOrder> ReturnProductOrders { get; set; } = [];
        public Cart Cart { get; set; } = new Cart();
        [Required][DataType(DataType.DateTime)] public DateTime CreatedDateTime { get; set; } = DateTime.Now;
        public ICollection<EditHistory> EditsHistory { get; set; } = [];
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Gender
    {
        Male, 
        Female
    }
}
