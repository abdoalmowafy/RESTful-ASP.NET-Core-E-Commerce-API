using ECommerce.Authentication.Contracts;
using ECommerce.Infrastructure.Abstractions;

namespace ECommerce.Authentication.Services;

public interface IAuthProfileService
{
    Task<Result<UserProfileResponse>> GetAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default);
    Task<Result<UserProfileResponse>> UpdateAsync(ClaimsPrincipal actor, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}

public class AuthProfileService(UserManager<ApplicationUser> userManager) : IAuthProfileService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<UserProfileResponse>> GetAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(actor.GetUserId());
        return user is null
            ? Result.Failure<UserProfileResponse>(UserErrors.NotFound)
            : Result.Succeed(ToResponse(user));
    }

    public async Task<Result<UserProfileResponse>> UpdateAsync(ClaimsPrincipal actor, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(actor.GetUserId());
        if (user is null)
            return Result.Failure<UserProfileResponse>(UserErrors.NotFound);

        var incomingPhone = request.PhoneNumber.ToE164();

        if (!string.IsNullOrWhiteSpace(incomingPhone)
            && !string.Equals(user.PhoneNumber, incomingPhone, StringComparison.OrdinalIgnoreCase)
            && _userManager.Users.Any(u => u.PhoneNumber == incomingPhone))
            return Result.Failure<UserProfileResponse>(UserErrors.PhoneDuplicated);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber.ToE164();
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = request.Gender;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure<UserProfileResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        return Result.Succeed(ToResponse(user));
    }

    private Task<ApplicationUser?> FindAsync(string userId)
        => _userManager.FindByIdAsync(userId);

    private static UserProfileResponse ToResponse(ApplicationUser user)
        => new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Gender);
}
