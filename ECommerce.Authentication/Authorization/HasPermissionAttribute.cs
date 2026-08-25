namespace ECommerce.Authentication.Authorization;

public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(policy: permission);
