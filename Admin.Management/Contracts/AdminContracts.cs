namespace Admin.Management.Contracts;

public record StoreManagementResponse(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string OwnerName,
    string OwnerEmail,
    StoreStatus Status,
    string? RejectionReason,
    int ProductsCount,
    DateTime CreatedAt);

public record UpdateStoreStatusRequest(StoreStatus Status, string? Reason = null);

public record AdminResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsDisabled,
    IReadOnlyList<string> Roles);

public record CreateAdminRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PhoneNumber);

public record UpdateAdminStatusRequest(bool Disabled);
