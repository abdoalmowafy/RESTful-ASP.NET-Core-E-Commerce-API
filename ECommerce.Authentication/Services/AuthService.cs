using ECommerce.Authentication.Contracts;
using ECommerce.Infrastructure.Abstractions;

namespace ECommerce.Authentication.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtProvider _jwtProvider = jwtProvider;

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        if (user.IsDisabled)
            return Result.Failure<AuthResponse>(UserErrors.Disabled);

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        if (!user.EmailConfirmed && !user.PhoneNumberConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.ContactNotConfirmed);

        var (token, expiresIn) = await _jwtProvider.GenerateTokenAsync(user, cancellationToken);
        var roles = await _userManager.GetRolesAsync(user);

        return Result.Succeed(new AuthResponse(
            token,
            expiresIn,
            user.Email!,
            user.FirstName,
            user.LastName,
            [.. roles]));
    }
}
