namespace ECommerce.Authentication.Contracts;

public record VerifyEmailRequest(string Email, string Code);
public record VerifyPhoneRequest(string Code);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);
