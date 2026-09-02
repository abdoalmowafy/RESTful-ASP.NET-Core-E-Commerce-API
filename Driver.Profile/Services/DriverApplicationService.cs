using ECommerce.Infrastructure.Entities.Enums;

using Driver.Profile.Contracts;
namespace Driver.Profile.Services;

public interface IDriverApplicationService
{
    Task<Result<DriverProfileResponse>> ApplyAsync(string userId, ApplyDriverForm form, CancellationToken cancellationToken = default);
    Task<Result<DriverProfileResponse>> ResubmitAsync(string userId, ApplyDriverForm form, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DriverProfileResponse>>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
}

public class DriverApplicationService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IFileStorage fileStorage) : IDriverApplicationService
{
    private static readonly string[] AllowedDocExtensions = [".jpg", ".jpeg", ".png", ".pdf"];
    private const long MaxDocBytes = 8 * 1024 * 1024;

    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IFileStorage _fileStorage = fileStorage;

    public async Task<Result<DriverProfileResponse>> ApplyAsync(string userId, ApplyDriverForm form, CancellationToken cancellationToken = default)
    {
        if (await _context.DriverProfiles.AnyAsync(p => p.Id == userId))
            return Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.AlreadyApplied);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<DriverProfileResponse>(UserErrors.NotFound);

        var docs = await SaveDocsAsync(userId, form, cancellationToken);
        if (docs.IsFailure)
            return Result.Failure<DriverProfileResponse>(docs.Error);

        var profile = new DriverProfile
        {
            Id = user.Id,
            RegistrationStatus = RegistrationStatus.PendingVerification,
            IsActive = false,
            VehicleType = form.VehicleType,
            PlateNumber = form.PlateNumber.Trim(),
            LicenseNumber = form.LicenseNumber.Trim(),
            LicenseImageUrl = docs.Value.LicenseImageUrl,
            VehicleRegistrationUrl = docs.Value.VehicleRegistrationUrl,
            NationalIdUrl = docs.Value.NationalIdUrl
        };

        user.DriverProfile = profile;
        await _userManager.UpdateAsync(user);

        return Result.Succeed(ToResponse(user));
    }

    public async Task<Result<DriverProfileResponse>> ResubmitAsync(string userId, ApplyDriverForm form, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .Include(u => u.DriverProfile)
            .FirstOrDefaultAsync(u => u.DriverProfile != null && u.Id == userId);

        if (user?.DriverProfile is null)
            return Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.NotFound);

        if (user.DriverProfile.RegistrationStatus != RegistrationStatus.Rejected)
            return Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.NotEditable);

        var profile = user.DriverProfile;
        var docs = await SaveDocsAsync(userId, form, cancellationToken);
        if (docs.IsFailure)
            return Result.Failure<DriverProfileResponse>(docs.Error);

        profile.VehicleType = form.VehicleType;
        profile.PlateNumber = form.PlateNumber.Trim();
        profile.LicenseNumber = form.LicenseNumber.Trim();
        profile.RegistrationStatus = RegistrationStatus.PendingVerification;
        profile.IsActive = false;
        profile.RejectionReason = null;
        profile.LicenseImageUrl = docs.Value.LicenseImageUrl ?? profile.LicenseImageUrl;
        profile.VehicleRegistrationUrl = docs.Value.VehicleRegistrationUrl ?? profile.VehicleRegistrationUrl;
        profile.NationalIdUrl = docs.Value.NationalIdUrl ?? profile.NationalIdUrl;

        await _userManager.UpdateAsync(user);
        return Result.Succeed(ToResponse(user));
    }

    public async Task<Result<IReadOnlyList<DriverProfileResponse>>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _context.DriverProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.RegistrationStatus == RegistrationStatus.PendingVerification)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new DriverProfileResponse(
                p.Id,
                $"{p.User!.FirstName} {p.User.LastName}".Trim(),
                p.User.Email!,
                p.VehicleType,
                p.PlateNumber,
                p.LicenseNumber,
                p.RegistrationStatus,
                p.RejectionReason))
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<DriverProfileResponse>>(pending);
    }

    private async Task<Result<(string? LicenseImageUrl, string? VehicleRegistrationUrl, string? NationalIdUrl)>> SaveDocsAsync(
        string userId,
        ApplyDriverForm form,
        CancellationToken ct)
    {
        try
        {
            var folder = $"media/drivers/{userId[..8]}";
            var license = await SaveOneAsync(form.LicenseImage, folder, ct);
            var registration = await SaveOneAsync(form.VehicleRegistration, folder, ct);
            var nationalId = await SaveOneAsync(form.NationalId, folder, ct);

            return Result.Succeed((license, registration, nationalId));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<(string?, string?, string?)>(Error.BadRequest("Driver.InvalidDocument", ex.Message));
        }
    }

    private async Task<string?> SaveOneAsync(IFormFile? file, string folder, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return null;

        var saved = await _fileStorage.SaveAllAsync([file], folder, MaxDocBytes, AllowedDocExtensions, ct);
        return saved[0].Url;
    }

    private static DriverProfileResponse ToResponse(ApplicationUser u)
        => new(
            u.DriverProfile!.Id,
            u.FullName,
            u.Email!,
            u.DriverProfile.VehicleType,
            u.DriverProfile.PlateNumber,
            u.DriverProfile.LicenseNumber,
            u.DriverProfile.RegistrationStatus,
            u.DriverProfile.RejectionReason);
}
