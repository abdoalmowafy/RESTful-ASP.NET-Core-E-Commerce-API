using System.Security.Claims;
using ECommerce.Infrastructure.Entities;

namespace ECommerce.Infrastructure.Extensions;

public static class UserExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? throw new UnauthorizedAccessException("User is not authenticated");

    public static List<string> GetRoleNames(this ClaimsPrincipal user)
    {
        var roleClaimType = user.Identity is ClaimsIdentity identity ? identity.RoleClaimType : "roles";
        return [.. user.Claims.Where(c => c.Type == roleClaimType).Select(c => c.Value)];
    }

    public static bool IsStaff(this ClaimsPrincipal user)
    {
        var roles = user.GetRoleNames();
        return roles.Contains("Admin") || roles.Contains("SuperAdmin");
    }
}
