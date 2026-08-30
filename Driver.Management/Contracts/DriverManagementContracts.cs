namespace Driver.Management.Contracts;

public record DriverManagementResponse(
    string Id,
    string FullName,
    string Email,
    VehicleType VehicleType,
    string PlateNumber,
    string LicenseNumber,
    RegistrationStatus Status,
    string? RejectionReason);

public record UpdateDriverStatusRequest(RegistrationStatus Status, string? Reason = null);
