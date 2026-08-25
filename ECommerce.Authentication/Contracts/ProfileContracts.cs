using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Authentication.Contracts;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber = null,
    DateOnly? DateOfBirth = null,
    Gender? Gender = null);

public record UserProfileResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    Gender? Gender);
