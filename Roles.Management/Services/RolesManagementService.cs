using Roles.Management.Contracts;

namespace Roles.Management.Services;

public interface IRolesManagementService
{
    Task<Result<IReadOnlyList<RoleResponse>>> GetAsync(CancellationToken cancellationToken = default);
    Task<Result<RoleResponse>> ReplacePermissionsAsync(string roleId, ReplaceRolePermissionsRequest request, CancellationToken cancellationToken = default);
    Task<Result> ReplaceUserRolesAsync(string userId, ReplaceUserRolesRequest request, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(string userId, AssignRoleRequest request, CancellationToken cancellationToken = default);
}

public class RolesManagementService(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IAuthPermissionService permissionCache) : IRolesManagementService
{
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IAuthPermissionService _permissionCache = permissionCache;

    public async Task<Result<IReadOnlyList<RoleResponse>>> GetAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var mapped = new List<RoleResponse>(roles.Count);

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            mapped.Add(new RoleResponse(
                role.Id,
                role.Name!,
                role.IsDefault,
                [.. claims.Where(c => c.Type == "permission").Select(c => c.Value).Order()]));
        }

        return Result.Succeed<IReadOnlyList<RoleResponse>>(mapped);
    }

    public async Task<Result<RoleResponse>> ReplacePermissionsAsync(
        string roleId,
        ReplaceRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        foreach (var permission in request.Permissions)
        {
            if (!permission.StartsWith(Permissions.Prefix, StringComparison.OrdinalIgnoreCase))
                return Result.Failure<RoleResponse>(Error.BadRequest("Roles.InvalidPermission", $"'{permission}' is not a valid permission"));
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
            return Result.Failure<RoleResponse>(AuthErrors.RoleNotFound);

        var existing = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existing.Where(c => c.Type == "permission"))
            await _roleManager.RemoveClaimAsync(role, claim);

        foreach (var permission in request.Permissions.Distinct())
            await _roleManager.AddClaimAsync(role, new Claim("permission", permission));

        await _permissionCache.InvalidateRoleCacheAsync(role.Id);

        return Result.Succeed(new RoleResponse(
            role.Id,
            role.Name!,
            role.IsDefault,
            [.. request.Permissions.Distinct().Order()]));
    }

    public async Task<Result> ReplaceUserRolesAsync(string userId, ReplaceUserRolesRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        foreach (var roleName in request.Roles)
        {
            if (await _roleManager.FindByNameAsync(roleName) is null)
                return Result.Failure(AuthErrors.RoleNotFound);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(request.Roles));
        await _userManager.AddToRolesAsync(user, request.Roles.Except(currentRoles));

        return Result.Succeed();
    }

    public async Task<Result> AssignRoleAsync(string userId, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (await _roleManager.FindByNameAsync(request.RoleName) is null)
            return Result.Failure(AuthErrors.RoleNotFound);

        await _userManager.AddToRoleAsync(user, request.RoleName);
        return Result.Succeed();
    }
}
