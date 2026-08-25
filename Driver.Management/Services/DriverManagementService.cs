using Driver.Management.Contracts;

namespace Driver.Management.Services;

public interface IDriverManagementService
{
    Task<Result<PaginatedList<DriverManagementResponse>>> GetAsync(DriverStatus? status, string? search, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusAsync(string driverId, UpdateDriverStatusRequest request, CancellationToken cancellationToken = default);
}

public class DriverManagementService(AppDbContext context, UserManager<ApplicationUser> userManager) : IDriverManagementService
{
    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<PaginatedList<DriverManagementResponse>>> GetAsync(
        DriverStatus? status,
        string? search,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.DriverProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => true);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.User!.FirstName.Contains(search) ||
                p.User.LastName.Contains(search) ||
                p.User.Email!.Contains(search));

        var page = await PaginatedList<DriverProfile>.CreateAsync(
            (IOrderedQueryable<DriverProfile>)query.OrderBy(p => p.CreatedAt),
            pageIndex, pageSize, cancellationToken);

        var mapped = page.Items.Select(p => new DriverManagementResponse(
            p.Id,
            $"{p.User!.FirstName} {p.User.LastName}".Trim(),
            p.User.Email!,
            p.VehicleType,
            p.PlateNumber,
            p.LicenseNumber,
            p.Status,
            p.RejectionReason)).ToList();

        return Result.Succeed(new PaginatedList<DriverManagementResponse>(mapped, page.PageNumber, page.TotalCount, pageSize));
    }

    public async Task<Result> UpdateStatusAsync(string driverId, UpdateDriverStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .Include(u => u.DriverProfile)
            .FirstOrDefaultAsync(u => u.DriverProfile != null && u.Id == driverId);

        if (user?.DriverProfile is null)
            return Result.Failure(MarketplaceErrors.DriverProfile.NotFound);

        if (request.Status == DriverStatus.PendingVerification)
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        if (request.Status == DriverStatus.Rejected && string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(Error.BadRequest("Driver.RejectionReasonRequired", "A rejection reason is required"));

        var profile = user.DriverProfile;
        if (profile.Status == request.Status)
            return Result.Succeed();

        if (!AllowedTransitions.TryGetValue(request.Status, out var allowedFrom) || !allowedFrom.Contains(profile.Status))
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        profile.Status = request.Status;
        profile.RejectionReason = request.Status == DriverStatus.Rejected ? request.Reason : null;
        user.IsDisabled = request.Status == DriverStatus.Suspended;

        await _userManager.UpdateAsync(user);
        return Result.Succeed();

    }

    private static readonly Dictionary<DriverStatus, DriverStatus[]> AllowedTransitions = new()
    {
        [DriverStatus.Active] = [DriverStatus.PendingVerification, DriverStatus.Suspended],
        [DriverStatus.Suspended] = [DriverStatus.Active],
        [DriverStatus.Rejected] = [DriverStatus.PendingVerification]
    };
}
