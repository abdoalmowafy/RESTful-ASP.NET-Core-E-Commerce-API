using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class CustomerProfile
{
    public string Id { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public ProfileStatus Status { get; set; } = ProfileStatus.Active;
    public int LoyaltyPoints { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AdminProfile
{
    public string Id { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SellerProfile
{
    public string Id { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int StoreId { get; set; }
    public Store? Store { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DriverProfile
{
    public string Id { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public VehicleType VehicleType { get; set; } = VehicleType.Motorcycle;
    public string PlateNumber { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string? LicenseImageUrl { get; set; }
    public string? VehicleRegistrationUrl { get; set; }
    public string? NationalIdUrl { get; set; }
    public DriverStatus Status { get; set; } = DriverStatus.PendingVerification;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
