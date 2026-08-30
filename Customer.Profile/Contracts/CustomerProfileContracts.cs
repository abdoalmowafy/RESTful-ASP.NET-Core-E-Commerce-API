namespace Customer.Profile.Contracts;

public record CustomerProfileResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    Gender? Gender,
    RegistrationStatus Status,
    int LoyaltyPoints);
