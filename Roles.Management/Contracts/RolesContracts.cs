namespace Roles.Management.Contracts;

public record RoleResponse(string Id, string Name, bool IsDefault, IReadOnlyList<string> Permissions);

public record ReplaceRolePermissionsRequest(IReadOnlyList<string> Permissions);

public record ReplaceUserRolesRequest(IReadOnlyList<string> Roles);

public record AssignRoleRequest(string RoleName);
