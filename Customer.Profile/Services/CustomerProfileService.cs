using Customer.Profile.Contracts;
using ECommerce.Infrastructure.Entities.Enums;

namespace Customer.Profile.Services;

public interface ICustomerProfileService
{
    Task<Result<CustomerProfileResponse>> GetAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}

public class CustomerProfileService(UserManager<ApplicationUser> userManager) : ICustomerProfileService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<CustomerProfileResponse>> GetAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .Include(u => u.CustomerProfile)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == actor.GetUserId());

        if (user?.CustomerProfile is null)
            return Result.Failure<CustomerProfileResponse>(MarketplaceErrors.Profiles.CustomerNotFound);

        return Result.Succeed(new CustomerProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Gender,
            user.CustomerProfile.RegistrationStatus,
            user.CustomerProfile.LoyaltyPoints));
    }
}
