using ECommerce.Authentication.Contracts;
using ECommerce.Authentication.Jwt;
using ECommerce.Authentication.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Authentication.Controllers;

[Route("auth")]
[ApiController]
public class AuthenticationController(
    IAuthRegistrationService registrationService,
    IAuthSessionService sessionService,
    IRefreshTokenService refreshTokenService,
    IAccountVerificationService verificationService,
    IAuthProfileService profileService,
    IAuthPasswordService passwordService,
    IAuthPermissionService permissionService,
    IOptions<RefreshTokenOptions> refreshOptions,
    IHostEnvironment environment) : ControllerBase
{
    private readonly IAuthRegistrationService _registrationService = registrationService;
    private readonly IAuthSessionService _sessionService = sessionService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly IAccountVerificationService _verificationService = verificationService;
    private readonly IAuthProfileService _profileService = profileService;
    private readonly IAuthPasswordService _passwordService = passwordService;
    private readonly IAuthPermissionService _permissionService = permissionService;
    private readonly RefreshTokenOptions _refreshOptions = refreshOptions.Value;
    private readonly IHostEnvironment _environment = environment;

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _registrationService.RegisterAsync(request, cancellationToken);
        if (!result.IsSucceed)
            return result.ToProblem();

        var otpResult = await _verificationService.SendEmailOtpAsync(request.Email, cancellationToken);
        return Ok(new
        {
            Message = "Registration successful. Check your email for a verification code.",
            EmailVerificationSent = otpResult.IsSucceed
        });
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _verificationService.VerifyEmailAsync(request.Email, request.Code, cancellationToken);
        return result.IsSucceed ? Ok(new { Message = "Email verified" }) : result.ToProblem();
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> ResendVerification([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _verificationService.SendEmailOtpAsync(request.Email, cancellationToken);
        return result.IsSucceed ? Ok(new { Message = "If the account exists, a code has been sent" }) : result.ToProblem();
    }

    [HttpPost("send-phone-otp")]
    [Authorize]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> SendPhoneOtp(CancellationToken cancellationToken)
    {
        var result = await _verificationService.SendPhoneOtpAsync(User, cancellationToken);
        return result.IsSucceed ? Ok(new { Message = "Code sent" }) : result.ToProblem();
    }

    [HttpPost("verify-phone")]
    [Authorize]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request, CancellationToken cancellationToken)
    {
        var result = await _verificationService.VerifyPhoneAsync(User, request.Code, cancellationToken);
        return result.IsSucceed ? Ok(new { Message = "Phone verified" }) : result.ToProblem();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _verificationService.ForgotPasswordAsync(request.Email, cancellationToken);
        return result.IsSucceed ? Ok(new { Message = "If the account exists, a reset code has been sent" }) : result.ToProblem();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _verificationService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword, cancellationToken);
        return result.IsSucceed ? Ok(new { Message = "Password reset. Please sign in again." }) : result.ToProblem();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var result = await _refreshTokenService.GetSessionsAsync(User.GetUserId(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("sessions/{familyId:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession([FromRoute] Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _refreshTokenService.RevokeFamilyForUserAsync(User.GetUserId(), familyId, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpDelete("sessions")]
    [Authorize]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeAllForUserAsync(User.GetUserId(), cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _sessionService.LoginWithSessionAsync(request, ip, cancellationToken);
        if (!result.IsSucceed)
            return result.ToProblem();

        SetRefreshCookie(result.Value.RefreshToken!);
        return Ok(WithoutRefreshToken(result.Value));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        var cookieToken = Request.Cookies[_refreshOptions.CookieName];
        var presented = !string.IsNullOrWhiteSpace(cookieToken) ? cookieToken : request?.RefreshToken;

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _sessionService.RefreshSessionAsync(presented, ip, cancellationToken);
        if (!result.IsSucceed)
        {
            DeleteRefreshCookie();
            return result.ToProblem();
        }

        if (!string.IsNullOrEmpty(result.Value.RefreshToken))
            SetRefreshCookie(result.Value.RefreshToken);

        return Ok(WithoutRefreshToken(result.Value));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        var cookieToken = Request.Cookies[_refreshOptions.CookieName];
        var presented = !string.IsNullOrWhiteSpace(cookieToken) ? cookieToken : request?.RefreshToken;

        await _sessionService.RevokeSessionAsync(presented, cancellationToken);
        DeleteRefreshCookie();

        return NoContent();
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetAsync(User, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateAsync(User, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _passwordService.ChangePasswordAsync(User, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpGet("permissions")]
    [Authorize]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _permissionService.GetPermissionsForUserAsync(User);
        return Ok(permissions);
    }

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(_refreshOptions.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = _refreshOptions.CookiePath,
            MaxAge = TimeSpan.FromDays(_refreshOptions.LifetimeDays)
        });
    }

    private void DeleteRefreshCookie()
        => Response.Cookies.Delete(_refreshOptions.CookieName, new CookieOptions
        {
            Path = _refreshOptions.CookiePath
        });

    private static AuthResponse WithoutRefreshToken(AuthResponse response)
        => response with { RefreshToken = null, RefreshTokenExpiresAtUtc = null };
}
