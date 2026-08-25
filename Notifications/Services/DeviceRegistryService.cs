using Notifications.Contracts;

namespace Notifications.Services;

public interface IDeviceRegistryService
{
    Task<Result> RegisterAsync(ClaimsPrincipal actor, string token, DevicePlatform platform, string? deviceName, CancellationToken cancellationToken = default);
    Task<Result> UnregisterAsync(ClaimsPrincipal actor, string token, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RegisteredDeviceResponse>>> GetMineAsync(ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}

public class DeviceRegistryService(
    AppDbContext context,
    IDeviceTokenService tokenService,
    UserManager<ApplicationUser> userManager) : IDeviceRegistryService
{
    private readonly AppDbContext _context = context;
    private readonly IDeviceTokenService _tokenService = tokenService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result> RegisterAsync(
        ClaimsPrincipal actor,
        string token,
        DevicePlatform platform,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        var userId = actor.GetUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var ownerType = ResolveOwnerType(await _userManager.GetRolesAsync(user));

        return await _tokenService.RegisterAsync(
            ownerType, userId, token, platform, deviceName, cancellationToken);
    }

    public async Task<Result> UnregisterAsync(
        ClaimsPrincipal actor,
        string token,
        CancellationToken cancellationToken = default)
    {
        var owns = await _context.DeviceTokens
            .AnyAsync(t => t.Token == token && t.OwnerId == actor.GetUserId(), cancellationToken);

        return owns
            ? await _tokenService.UnregisterAsync(token, cancellationToken).ContinueWith(_ => Result.Succeed())
            : Result.Failure(Error.NotFound("Notifications.TokenNotFound", "Device token not found"));
    }

    public async Task<Result<IReadOnlyList<RegisteredDeviceResponse>>> GetMineAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        var devices = await _context.DeviceTokens
            .AsNoTracking()
            .Where(t => t.OwnerId == actor.GetUserId())
            .OrderByDescending(t => t.LastRegisteredAtUtc)
            .Select(t => new RegisteredDeviceResponse(t.Id, t.Platform, t.DeviceName, t.LastRegisteredAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<RegisteredDeviceResponse>>(devices);
    }

    private static AppOwnerType ResolveOwnerType(IList<string> roles)
    {
        if (roles.Contains(DefaultRoles.Driver)) return AppOwnerType.Driver;
        if (roles.Contains(DefaultRoles.Seller)) return AppOwnerType.Seller;
        if (roles.Contains(DefaultRoles.Admin) || roles.Contains(DefaultRoles.SuperAdmin)) return AppOwnerType.Admin;
        return AppOwnerType.Customer;
    }
}
