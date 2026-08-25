using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Authentication.Contracts;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber = null,
    DateOnly? DateOfBirth = null,
    Gender? Gender = null);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    int ExpiresIn,
    string Email,
    string FirstName,
    string LastName,
    string[] Roles,
    string? RefreshToken = null,
    DateTime? RefreshTokenExpiresAtUtc = null);

public record RefreshRequest(string? RefreshToken = null);
