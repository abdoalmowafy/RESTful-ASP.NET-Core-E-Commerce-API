using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<Address> Addresses { get; set; } = [];
    [JsonIgnore]
    public Cart? Cart { get; set; }
    [JsonIgnore]
    public CustomerProfile? CustomerProfile { get; set; }
    [JsonIgnore]
    public AdminProfile? AdminProfile { get; set; }
    [JsonIgnore]
    public SellerProfile? SellerProfile { get; set; }
    [JsonIgnore]
    public DriverProfile? DriverProfile { get; set; }
    [JsonIgnore]
    public Store? Store { get; set; }
    [JsonIgnore]
    public ICollection<Product> WishList { get; set; } = [];
    [JsonIgnore]
    public ICollection<Order> Orders { get; set; } = [];
    [JsonIgnore]
    public ICollection<ReturnRequest> ReturnsRequested { get; set; } = [];
    [JsonIgnore]
    public ICollection<ReturnRequest> ReturnsTransported { get; set; } = [];
    [JsonIgnore]
    public ICollection<Order> DeliveriesAssigned { get; set; } = [];
    [JsonIgnore]
    public ICollection<Review> Reviews { get; set; } = [];
    [JsonIgnore]
    public ICollection<Search> Searches { get; set; } = [];
}
