namespace Customer.Management.Contracts;

public record CustomerManagementResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ProfileStatus Status,
    bool IsDisabled,
    DateTime CreatedAt);

public record UpdateCustomerStatusRequest(ProfileStatus Status);
