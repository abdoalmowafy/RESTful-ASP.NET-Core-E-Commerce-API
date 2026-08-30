namespace Driver.Profile.Contracts;

public record DriverProfileResponse(
    string Id,
    string FullName,
    string Email,
    VehicleType VehicleType,
    string PlateNumber,
    string LicenseNumber,
    RegistrationStatus Status,
    string? RejectionReason);

public record ApplyDriverRequest(VehicleType VehicleType, string PlateNumber, string LicenseNumber);

public record ApplyDriverForm(
    VehicleType VehicleType,
    string PlateNumber,
    string LicenseNumber,
    IFormFile? LicenseImage = null,
    IFormFile? VehicleRegistration = null,
    IFormFile? NationalId = null);

public record DeliveryResponse(
    int OrderId,
    string CustomerName,
    string City,
    string FullAddress,
    long TotalCents,
    int ItemsCount,
    DateTime CreatedAt);

public record PickupResponse(
    int ReturnId,
    string CustomerName,
    string City,
    string FullAddress,
    string ProductName,
    int Quantity,
    DateTime CreatedAt);

public record UpdateDriverLocationRequest(double Latitude, double Longitude);
