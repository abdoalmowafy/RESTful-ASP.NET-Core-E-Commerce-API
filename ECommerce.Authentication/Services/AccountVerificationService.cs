using ECommerce.Authentication.Contracts;
using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Authentication.Services;

public interface IAccountVerificationService
{
    Task<Result> SendEmailOtpAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> VerifyEmailAsync(string email, string code, CancellationToken cancellationToken = default);
    Task<Result> SendPhoneOtpAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default);
    Task<Result> VerifyPhoneAsync(ClaimsPrincipal actor, string code, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default);
}

public class AccountVerificationService(
    UserManager<ApplicationUser> userManager,
    IOtpChallengeService otpChallengeService,
    INotificationDelivery delivery,
    AppDbContext context,
    IRefreshTokenService refreshTokenService) : IAccountVerificationService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IOtpChallengeService _otp = otpChallengeService;
    private readonly INotificationDelivery _delivery = delivery;
    private readonly AppDbContext _context = context;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;

    public async Task<Result> SendEmailOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is not null && !user.EmailConfirmed)
        {
            var otp = await _otp.IssueAsync(OtpPurpose.EmailVerification, email, cancellationToken);
            await _delivery.SendEmailAsync(
                email,
                "Verify your StoreFront account",
                $"Your verification code is {otp.Code}. It expires in 5 minutes.");
        }

        return Result.Succeed();
    }

    public async Task<Result> VerifyEmailAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.Failure(AuthErrors.InvalidRefreshToken with { Code = "Auth.InvalidVerification", Description = "Invalid verification request" });

        if (!await _otp.ValidateAndConsumeAsync(OtpPurpose.EmailVerification, email, code, cancellationToken))
            return Result.Failure(Error.BadRequest("Auth.InvalidOtp", "Invalid or expired verification code"));

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
        return Result.Succeed();
    }

    public async Task<Result> SendPhoneOtpAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(actor.GetUserId());
        if (user?.PhoneNumber is null)
            return Result.Failure(Error.BadRequest("Auth.NoPhoneNumber", "Add a phone number to your profile first"));

        var otp = await _otp.IssueAsync(OtpPurpose.PhoneVerification, user.PhoneNumber, cancellationToken);
        await _delivery.SendSmsAsync(user.PhoneNumber, $"Your StoreFront verification code is {otp.Code}.");
        return Result.Succeed();
    }

    public async Task<Result> VerifyPhoneAsync(ClaimsPrincipal actor, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(actor.GetUserId());
        if (user?.PhoneNumber is null)
            return Result.Failure(Error.BadRequest("Auth.NoPhoneNumber", "Add a phone number to your profile first"));

        if (!await _otp.ValidateAndConsumeAsync(OtpPurpose.PhoneVerification, user.PhoneNumber, code, cancellationToken))
            return Result.Failure(Error.BadRequest("Auth.InvalidOtp", "Invalid or expired verification code"));

        user.PhoneNumberConfirmed = true;
        await _userManager.UpdateAsync(user);
        return Result.Succeed();
    }

    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            var otp = await _otp.IssueAsync(OtpPurpose.PasswordReset, email, cancellationToken);
            await _delivery.SendEmailAsync(
                email,
                "Reset your StoreFront password",
                $"Your password reset code is {otp.Code}. It expires in 5 minutes. If you did not request this, ignore this message.");
        }

        return Result.Succeed();
    }

    public async Task<Result> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.Failure(Error.BadRequest("Auth.InvalidReset", "Invalid reset request"));

        if (!await _otp.ValidateAndConsumeAsync(OtpPurpose.PasswordReset, email, code, cancellationToken))
            return Result.Failure(Error.BadRequest("Auth.InvalidOtp", "Invalid or expired reset code"));

        var result = await _userManager.ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user), newPassword);
        if (!result.Succeeded)
            return Result.Failure(new Error(result.Errors.First().Code, result.Errors.First().Description, StatusCodes.Status400BadRequest));

        await RevokeAllSessionsAsync(user.Id, cancellationToken);
        return Result.Succeed();
    }

    private async Task RevokeAllSessionsAsync(string userId, CancellationToken cancellationToken)
    {
        var active = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
            token.RevokedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
