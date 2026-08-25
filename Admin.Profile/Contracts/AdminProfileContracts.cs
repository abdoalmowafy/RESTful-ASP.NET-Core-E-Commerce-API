namespace Admin.Profile.Contracts;

public record UpdateAdminProfileRequest(string? JobTitle, string? Department);

public record AdminProfileResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? JobTitle,
    string? Department);
