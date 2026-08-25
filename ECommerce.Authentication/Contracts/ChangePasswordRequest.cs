namespace ECommerce.Authentication.Contracts;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
