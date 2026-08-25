using Admin.Profile.Contracts;

namespace Admin.Profile.Services;

public interface IAdminProfileService
{
    Task<Result<AdminProfileResponse>> GetAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default);
    Task<Result<AdminProfileResponse>> UpdateAsync(ClaimsPrincipal actor, UpdateAdminProfileRequest request, CancellationToken cancellationToken = default);
}

public class AdminProfileService(UserManager<ApplicationUser> userManager) : IAdminProfileService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<AdminProfileResponse>> GetAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(actor.GetUserId());
        return user is null
            ? Result.Failure<AdminProfileResponse>(MarketplaceErrors.Profiles.AdminNotFound)
            : Result.Succeed(ToResponse(user));
    }

    public async Task<Result<AdminProfileResponse>> UpdateAsync(ClaimsPrincipal actor, UpdateAdminProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(actor.GetUserId());
        if (user is null)
            return Result.Failure<AdminProfileResponse>(MarketplaceErrors.Profiles.AdminNotFound);

        user.AdminProfile!.JobTitle = request.JobTitle?.Trim();
        user.AdminProfile.Department = request.Department?.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Failure<AdminProfileResponse>(new Error(result.Errors.First().Code, result.Errors.First().Description, StatusCodes.Status400BadRequest));

        return Result.Succeed(ToResponse(user));
    }

    private async Task<ApplicationUser?> LoadAsync(string userId)
        => await _userManager.Users
            .Include(u => u.AdminProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

    private static AdminProfileResponse ToResponse(ApplicationUser u)
        => new(u.Id, u.FirstName, u.LastName, u.Email!, u.AdminProfile!.JobTitle, u.AdminProfile.Department);
}
