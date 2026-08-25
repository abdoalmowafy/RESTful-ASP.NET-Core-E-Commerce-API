namespace ECommerce.Infrastructure.Errors;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "User not found");
    public static readonly Error InvalidCredentials = Error.Unauthorized("User.InvalidCredentials", "Invalid email or password");
    public static readonly Error EmailDuplicated = Error.Conflict("User.EmailDuplicated", "Email is already registered");
    public static readonly Error PhoneDuplicated = Error.Conflict("User.PhoneDuplicated", "Phone number is already in use");
    public static readonly Error Disabled = Error.Forbidden("User.Disabled", "This account has been disabled");
    public static readonly Error ContactNotConfirmed = Error.Forbidden("User.ContactNotConfirmed", "Confirm your email and phone number first");
}

public static class AuthErrors
{
    public static readonly Error RoleNotFound = Error.NotFound("Auth.RoleNotFound", "Role not found");
    public static readonly Error InvalidRefreshToken = Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token");
}
