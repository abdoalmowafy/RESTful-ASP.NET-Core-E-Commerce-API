using ECommerce.Authentication.Contracts;
using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;

namespace ECommerce.Authentication.Services;

public interface IAuthSessionService
{
    Task<Result<AuthResponse>> LoginWithSessionAsync(LoginRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshSessionAsync(string? presentedToken, string? ip, CancellationToken cancellationToken = default);
    Task<Result> RevokeSessionAsync(string? presentedToken, CancellationToken cancellationToken = default);
}

public class AuthSessionService(
    IAuthService authService,
    IRefreshTokenService refreshTokenService,
    IJwtProvider jwtProvider,
    UserManager<ApplicationUser> userManager) : IAuthSessionService
{
    private readonly IAuthService _authService = authService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly IJwtProvider _jwtProvider = jwtProvider;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<AuthResponse>> LoginWithSessionAsync(
        LoginRequest request,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var login = await _authService.LoginAsync(request, cancellationToken);
        if (login.IsFailure)
            return Result.Failure<AuthResponse>(login.Error);

        var user = await _userManager.FindByEmailAsync(request.Email);
        var issued = await _refreshTokenService.IssueAsync(user!, ip, cancellationToken: cancellationToken);

        return Result.Succeed(WithRefresh(login.Value, issued.Token, issued.ExpiresAtUtc));
    }

    public async Task<Result<AuthResponse>> RefreshSessionAsync(
        string? presentedToken,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presentedToken))
            return Result.Failure<AuthResponse>(AuthErrors.InvalidRefreshToken);

        var rotated = await _refreshTokenService.RotateAsync(presentedToken, ip, cancellationToken);
        if (rotated.IsFailure)
            return Result.Failure<AuthResponse>(rotated.Error);

        var user = rotated.Value.User;

        if (user.IsDisabled)
            return Result.Failure<AuthResponse>(UserErrors.Disabled);

        if (!user.EmailConfirmed && !user.PhoneNumberConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.ContactNotConfirmed);

        var (token, expiresIn) = await _jwtProvider.GenerateTokenAsync(user, cancellationToken);
        var roles = await _userManager.GetRolesAsync(user);

        return Result.Succeed(WithRefresh(new AuthResponse(
            token, expiresIn, user.Email!, user.FirstName, user.LastName, [.. roles]),
            rotated.Value.Token == string.Empty ? null : rotated.Value.Token,
            rotated.Value.Token == string.Empty ? null : rotated.Value.ExpiresAtUtc));
    }

    public async Task<Result> RevokeSessionAsync(string? presentedToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(presentedToken))
            await _refreshTokenService.RevokeFamilyAsync(presentedToken, cancellationToken);

        return Result.Succeed();
    }

    private static AuthResponse WithRefresh(AuthResponse response, string? refreshToken, DateTime? expiresAtUtc)
        => response with { RefreshToken = refreshToken, RefreshTokenExpiresAtUtc = expiresAtUtc };
}
