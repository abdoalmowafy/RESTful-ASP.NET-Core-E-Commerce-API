namespace Driver.Management.Contracts;

public record DriverManagementResponse(
    string Id,
    string FullName,
    string Email,
    VehicleType VehicleType,
    string PlateNumber,
    string LicenseNumber,
    DriverStatus Status,
    string? RejectionReason);

public record UpdateDriverStatusRequest(DriverStatus Status, string? Reason = null);
