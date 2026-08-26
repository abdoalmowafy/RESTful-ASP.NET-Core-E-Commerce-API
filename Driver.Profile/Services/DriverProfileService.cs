using Driver.Profile.Contracts;

namespace Driver.Profile.Services;

public interface IDriverProfileService
{
    Task<Result<DriverProfileResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<DriverProfileResponse>> ApplyAsync(string userId, ApplyDriverRequest request, CancellationToken cancellationToken = default);
    Task<Result<DriverProfileResponse>> ResubmitAsync(string userId, ApplyDriverRequest request, CancellationToken cancellationToken = default);
}

public class DriverProfileService(UserManager<ApplicationUser> userManager) : IDriverProfileService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<DriverProfileResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(userId);
        return user?.DriverProfile is null
            ? Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.NotFound)
            : Result.Succeed(ToResponse(user));
    }

    public async Task<Result<DriverProfileResponse>> ApplyAsync(string userId, ApplyDriverRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userManager.Users.AnyAsync(u => u.DriverProfile != null && u.Id == userId))
            return Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.AlreadyApplied);

        var user = await _userManager.Users.Include(u => u.DriverProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Result.Failure<DriverProfileResponse>(UserErrors.NotFound);

        await _userManager.AddToRoleAsync(user, "Driver");

        user.DriverProfile = new DriverProfile
        {
            Id = user.Id,
            VehicleType = request.VehicleType,
            PlateNumber = request.PlateNumber,
            LicenseNumber = request.LicenseNumber,
            Status = DriverStatus.PendingVerification
        };

        await _userManager.UpdateAsync(user);
        return Result.Succeed(ToResponse(user));
    }

    public async Task<Result<DriverProfileResponse>> ResubmitAsync(string userId, ApplyDriverRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(userId);
        if (user?.DriverProfile is null)
            return Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.NotFound);

        if (user.DriverProfile.Status != DriverStatus.Rejected)
            return Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.NotEditable);

        user.DriverProfile.VehicleType = request.VehicleType;
        user.DriverProfile.PlateNumber = request.PlateNumber;
        user.DriverProfile.LicenseNumber = request.LicenseNumber;
        user.DriverProfile.Status = DriverStatus.PendingVerification;
        user.DriverProfile.RejectionReason = null;

        await _userManager.UpdateAsync(user);
        return Result.Succeed(ToResponse(user));
    }

    private async Task<ApplicationUser?> LoadAsync(string userId)
        => await _userManager.Users
            .Include(u => u.DriverProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

    private static DriverProfileResponse ToResponse(ApplicationUser u)
        => new(
            u.DriverProfile!.Id,
            $"{u.FirstName} {u.LastName}".Trim(),
            u.Email!,
            u.DriverProfile.VehicleType,
            u.DriverProfile.PlateNumber,
            u.DriverProfile.LicenseNumber,
            u.DriverProfile.Status,
            u.DriverProfile.RejectionReason);
}
