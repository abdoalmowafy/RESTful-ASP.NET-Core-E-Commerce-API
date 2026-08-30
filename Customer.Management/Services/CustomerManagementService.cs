using Customer.Management.Contracts;
using ECommerce.Infrastructure.Entities.Enums;

namespace Customer.Management.Services;

public interface ICustomerManagementService
{
    Task<Result<PaginatedList<CustomerManagementResponse>>> GetAsync(string? search, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusAsync(string customerId, UpdateCustomerStatusRequest request, CancellationToken cancellationToken = default);
}

public class CustomerManagementService(UserManager<ApplicationUser> userManager) : ICustomerManagementService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<PaginatedList<CustomerManagementResponse>>> GetAsync(
        string? search,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _userManager.Users
            .AsNoTracking()
            .Include(u => u.CustomerProfile)
            .Where(u => u.CustomerProfile != null);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email!.Contains(search));

        var page = await PaginatedList<ApplicationUser>.CreateAsync(
            (IOrderedQueryable<ApplicationUser>)query.OrderBy(u => u.CreatedAt),
            pageIndex, pageSize, cancellationToken);

        var mapped = page.Items.Select(u => new CustomerManagementResponse(
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email!,
            u.PhoneNumber,
            u.CustomerProfile!.RegistrationStatus,
            u.IsDisabled,
            u.CreatedAt)).ToList();

        return Result.Succeed(new PaginatedList<CustomerManagementResponse>(mapped, page.PageNumber, page.TotalCount, pageSize));
    }

    public async Task<Result> UpdateStatusAsync(string customerId, UpdateCustomerStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .Include(u => u.CustomerProfile)
            .FirstOrDefaultAsync(u => u.CustomerProfile != null && u.Id == customerId);

        if (user is null)
            return Result.Failure(MarketplaceErrors.Profiles.CustomerNotFound);

        user.CustomerProfile!.RegistrationStatus = request.Status;
        user.IsDisabled = request.Status == RegistrationStatus.Rejected;

        await _userManager.UpdateAsync(user);
        return Result.Succeed();
    }
}
