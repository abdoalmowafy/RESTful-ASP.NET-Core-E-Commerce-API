using ECommerce.Infrastructure.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.Authentication.Authorization;

/// <summary>Requires email_confirmed OR phone_number_confirmed claim.</summary>
public sealed class VerifiedUserRequirement : IAuthorizationRequirement;

/// <summary>
/// Requires a "<c>{ClaimType}</c>" status claim whose value is one of the
/// allowed profile statuses. Claim values are written by JwtProvider from
/// the audience profile tables (CustomerProfile / Store / DriverProfile).
/// </summary>
public sealed class ProfileStatusRequirement(string claimType, params string[] allowedStatuses) : IAuthorizationRequirement
{
    public string ClaimType { get; } = claimType;
    public string[] AllowedStatuses { get; } = allowedStatuses;
}

public static class ProfileClaims
{
    public const string CustomerStatus = "customer_status";
    public const string StoreStatus = "store_status";
    public const string DriverStatus = "driver_status";
}

public sealed class VerifiedUserHandler : AuthorizationHandler<VerifiedUserRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifiedUserRequirement requirement)
    {
        var emailConfirmed = context.User.HasClaim("email_confirmed", "true");
        var phoneConfirmed = context.User.HasClaim("phone_number_confirmed", "true");

        if (emailConfirmed || phoneConfirmed)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class ProfileStatusHandler : AuthorizationHandler<ProfileStatusRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProfileStatusRequirement requirement)
    {
        foreach (var status in requirement.AllowedStatuses)
        {
            if (context.User.HasClaim(requirement.ClaimType, status))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
