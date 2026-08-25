using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;

using ECommerce.Infrastructure.Persistence;
namespace ECommerce.Infrastructure.Services;

public record RegisteredDevice(Guid Id, DevicePlatform Platform, string? DeviceName, DateTime LastRegisteredAtUtc);

public interface IDeviceTokenService
{
    Task<Result<Guid>> RegisterAsync(AppOwnerType ownerType, string ownerId, string token, DevicePlatform platform, string? deviceName, CancellationToken cancellationToken = default);
    Task UnregisterAsync(string token, CancellationToken cancellationToken = default);
    Task RemoveDeadTokensAsync(IEnumerable<string> deadTokens, CancellationToken cancellationToken = default);
}

/// <summary>
/// Registry of FCM tokens. Upsert-by-token: re-registration refreshes
/// LastRegisteredAtUtc so stale installs can be pruned.
/// </summary>
public class DeviceTokenService(AppDbContext context) : IDeviceTokenService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<Guid>> RegisterAsync(
        AppOwnerType ownerType,
        string ownerId,
        string token,
        DevicePlatform platform,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (existing is not null)
        {
            existing.OwnerType = ownerType;
            existing.OwnerId = ownerId;
            existing.Platform = platform;
            existing.DeviceName = deviceName;
            existing.MarkRegistered();
        }
        else
        {
            _context.DeviceTokens.Add(new DeviceToken
            {
                OwnerType = ownerType,
                OwnerId = ownerId,
                Token = token,
                Platform = platform,
                DeviceName = deviceName
            });
        }

                try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // Lost the upsert race — the concurrent insert won. Refresh ours and fall through.
            _context.ChangeTracker.Clear();
            existing = await _context.DeviceTokens.FirstAsync(t => t.Token == token, cancellationToken);
            existing.OwnerType = ownerType;
            existing.OwnerId = ownerId;
            existing.Platform = platform;
            existing.DeviceName = deviceName;
            existing.MarkRegistered();
            await _context.SaveChangesAsync(cancellationToken);
        }

await _context.SaveChangesAsync(cancellationToken);

        var id = existing?.Id
            ?? await _context.DeviceTokens.Where(t => t.Token == token).Select(t => t.Id).FirstAsync(cancellationToken);
        return Result.Succeed(id);
    }

    public async Task UnregisterAsync(string token, CancellationToken cancellationToken = default)
    {
        var dead = await _context.DeviceTokens
            .Where(t => t.Token == token)
            .ToListAsync(cancellationToken);

        _context.DeviceTokens.RemoveRange(dead);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDeadTokensAsync(IEnumerable<string> deadTokens, CancellationToken cancellationToken = default)
    {
        var hashes = deadTokens.ToHashSet();
        if (hashes.Count == 0) return;

        var dead = await _context.DeviceTokens
            .Where(t => hashes.Contains(t.Token))
            .ToListAsync(cancellationToken);

        _context.DeviceTokens.RemoveRange(dead);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
