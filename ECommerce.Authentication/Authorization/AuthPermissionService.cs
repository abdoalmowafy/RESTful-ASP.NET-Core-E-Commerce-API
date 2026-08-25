using System.Security.Claims;
using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Services;

namespace ECommerce.Authentication.Authorization;

public interface IAuthPermissionService
{
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
    Task<string[]> GetPermissionsForUserAsync(ClaimsPrincipal user);
    Task InvalidateRoleCacheAsync(string roleId);
}

public class AuthPermissionService(ICacheService cacheService, RoleManager<ApplicationRole> roleManager) : IAuthPermissionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
    {
        foreach (var roleName in GetRoleNames(user))
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var permissions = await GetCachedPermissionsAsync(role);
            if (permissions.Contains(permission))
                return true;
        }

        return false;
    }

    public async Task<string[]> GetPermissionsForUserAsync(ClaimsPrincipal user)
    {
        var allPermissions = new HashSet<string>();

        foreach (var roleName in GetRoleNames(user))
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var permissions = await GetCachedPermissionsAsync(role);
            allPermissions.UnionWith(permissions);
        }

        return [.. allPermissions];
    }

    public async Task InvalidateRoleCacheAsync(string roleId)
    {
        try
        {
            await cacheService.RemoveAsync($"role:{roleId}:permissions");
        }
        catch
        {
        }
    }

    private static List<string> GetRoleNames(ClaimsPrincipal user)
    {
        var roleClaimType = user.Identity is ClaimsIdentity identity ? identity.RoleClaimType : "roles";
        return [.. user.Claims.Where(c => c.Type == roleClaimType).Select(c => c.Value)];
    }

    private async Task<string[]> GetCachedPermissionsAsync(ApplicationRole role)
    {
        var cacheKey = $"role:{role.Id}:permissions";

        try
        {
            var cached = await cacheService.GetAsync<string[]>(cacheKey);
            if (cached is not null)
                return cached;
        }
        catch
        {
        }

        var permissions = (await roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToArray();

        try
        {
            await cacheService.SetAsync(cacheKey, permissions, CacheTtl);
        }
        catch
        {
        }

        return permissions;
    }
}
