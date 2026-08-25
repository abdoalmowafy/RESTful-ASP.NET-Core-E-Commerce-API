using Roles.Management.Contracts;
using Roles.Management.Services;

namespace Roles.Management.Controllers;

[Route("admin/roles")]
[ApiController]
[Authorize]
public class RolesController(IRolesManagementService rolesService) : ControllerBase
{
    private readonly IRolesManagementService _rolesService = rolesService;

    [HttpGet]
    [HasPermission(Permissions.Admins.View)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _rolesService.GetAsync(cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{roleId}/permissions")]
    [HasPermission(Permissions.Admins.Manage)]
    public async Task<IActionResult> ReplacePermissions(
        [FromRoute] string roleId,
        [FromBody] ReplaceRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rolesService.ReplacePermissionsAsync(roleId, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("users/{userId}/roles")]
    [HasPermission(Permissions.Admins.Manage)]
    public async Task<IActionResult> ReplaceUserRoles(
        [FromRoute] string userId,
        [FromBody] ReplaceUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rolesService.ReplaceUserRolesAsync(userId, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpPost("users/{userId}/roles")]
    [HasPermission(Permissions.Admins.Manage)]
    public async Task<IActionResult> AssignRole(
        [FromRoute] string userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rolesService.AssignRoleAsync(userId, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}
