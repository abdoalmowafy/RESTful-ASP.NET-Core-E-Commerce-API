using System.Security.Claims;

namespace ECommerce.UnitTests.Infrastructure;

public static class TestActor
{
    public static ClaimsPrincipal For(string userId)
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.NameIdentifier, "roles-placeholder")
        ], authenticationType: "test"));
}

public static class ClaimsPrincipalExtensionsForTests
{
    public static ClaimsPrincipal WithoutId(this ClaimsPrincipal principal)
        => new(new ClaimsIdentity([], authenticationType: "test"));
}
