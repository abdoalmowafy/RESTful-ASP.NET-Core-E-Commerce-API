using Driver.Profile.Contracts;
using Microsoft.AspNetCore.Hosting;

namespace Driver.Profile.Services;

public interface IDriverApplicationService
{
    Task<Result<DriverProfileResponse>> ApplyAsync(string userId, ApplyDriverForm form, CancellationToken cancellationToken = default);
    Task<Result<DriverProfileResponse>> ResubmitAsync(string userId, ApplyDriverForm form, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DriverProfileResponse>>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
}

public record ApplyDriverForm(
    VehicleType VehicleType,
    string PlateNumber,
    string LicenseNumber,
    IFormFile? LicenseImage,
    IFormFile? VehicleRegistration,
    IFormFile? NationalId);

public class DriverApplicationService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment) : IDriverApplicationService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".pdf"];
    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly string _docsFolder = Path.Combine(
        environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
        "media", "drivers");

    public async Task<Result<DriverProfileResponse>> ApplyAsync(string userId, ApplyDriverForm form, CancellationToken cancellationToken = default)
    {
        if (await _context.DriverProfiles.AnyAsync(p => p.Id == userId))
            return Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.AlreadyApplied);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<DriverProfileResponse>(UserErrors.NotFound);

        await _userManager.AddToRoleAsync(user, DefaultRoles.Driver);

        var profile = new DriverProfile
        {
            Id = user.Id,
            Status = DriverStatus.PendingVerification,
            VehicleType = form.VehicleType,
            PlateNumber = form.PlateNumber.Trim(),
            LicenseNumber = form.LicenseNumber.Trim()
        };

        profile.LicenseImageUrl = await SaveDocAsync(form.LicenseImage, $"license-{userId[..8]}");
        profile.VehicleRegistrationUrl = await SaveDocAsync(form.VehicleRegistration, $"registration-{userId[..8]}");
        profile.NationalIdUrl = await SaveDocAsync(form.NationalId, $"nationalid-{userId[..8]}");

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

        if (user.DriverProfile.Status != DriverStatus.Rejected)
            return Result.Failure<DriverProfileResponse>(MarketplaceErrors.DriverProfile.NotEditable);

        var profile = user.DriverProfile;
        profile.VehicleType = form.VehicleType;
        profile.PlateNumber = form.PlateNumber.Trim();
        profile.LicenseNumber = form.LicenseNumber.Trim();
        profile.Status = DriverStatus.PendingVerification;
        profile.RejectionReason = null;

        var license = await SaveDocAsync(form.LicenseImage, $"license-{userId[..8]}");
        if (license is not null) profile.LicenseImageUrl = license;

        var registration = await SaveDocAsync(form.VehicleRegistration, $"registration-{userId[..8]}");
        if (registration is not null) profile.VehicleRegistrationUrl = registration;

        var nationalId = await SaveDocAsync(form.NationalId, $"nationalid-{userId[..8]}");
        if (nationalId is not null) profile.NationalIdUrl = nationalId;

        await _userManager.UpdateAsync(user);
        return Result.Succeed(ToResponse(user));
    }

    public async Task<Result<IReadOnlyList<DriverProfileResponse>>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _context.DriverProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.Status == DriverStatus.PendingVerification)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new DriverProfileResponse(
                p.Id,
                $"{p.User!.FirstName} {p.User.LastName}".Trim(),
                p.User.Email!,
                p.VehicleType,
                p.PlateNumber,
                p.LicenseNumber,
                p.Status,
                p.RejectionReason))
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<DriverProfileResponse>>(pending);
    }

    private async Task<string?> SaveDocAsync(IFormFile? file, string baseName)
    {
        if (file is null || file.Length == 0)
            return null;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException($"Unsupported document type '{extension}'");

        Directory.CreateDirectory(_docsFolder);
        var fileName = $"{baseName}-{Guid.NewGuid():N}{extension}";

        await using var stream = File.Create(Path.Combine(_docsFolder, fileName));
        await file.CopyToAsync(stream);

        return $"/media/drivers/{fileName}";
    }

    private static DriverProfileResponse ToResponse(ApplicationUser u)
        => new(
            u.DriverProfile!.Id,
            u.FullName,
            u.Email!,
            u.DriverProfile.VehicleType,
            u.DriverProfile.PlateNumber,
            u.DriverProfile.LicenseNumber,
            u.DriverProfile.Status,
            u.DriverProfile.RejectionReason);
}
