using System.Security.Cryptography;
using ECommerce.Infrastructure.Abstractions;

namespace ECommerce.Authentication.Services;

public record IssuedRefreshToken(string Token, DateTime ExpiresAtUtc);
public record RotatedRefreshToken(ApplicationUser User, string Token, DateTime ExpiresAtUtc);

public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> IssueAsync(ApplicationUser user, string? ip = null, Guid? familyId = null, CancellationToken cancellationToken = default);
    Task<Result<RotatedRefreshToken>> RotateAsync(string presentedToken, string? ip = null, CancellationToken cancellationToken = default);
    Task RevokeFamilyAsync(string presentedToken, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SessionResponse>>> GetSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result> RevokeFamilyForUserAsync(string userId, Guid familyId, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}

public record SessionResponse(Guid FamilyId, DateTime CreatedAtUtc, DateTime ExpiresAtUtc, DateTime? LastUsedAtUtc);

public class RefreshTokenService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IOptions<Jwt.RefreshTokenOptions> options,
    Func<DateTime>? utcNowFactory = null) : IRefreshTokenService
{
    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly Jwt.RefreshTokenOptions _options = options.Value;
    private readonly Func<DateTime> _utcNow = utcNowFactory ?? (() => DateTime.UtcNow);

    public async Task<IssuedRefreshToken> IssueAsync(
        ApplicationUser user,
        string? ip = null,
        Guid? familyId = null,
        CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = _utcNow();

        var entity = new RefreshToken
        {
            TokenHash = Hash(token),
            FamilyId = familyId ?? Guid.NewGuid(),
            UserId = user.Id,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_options.LifetimeDays),
            CreatedByIp = ip
        };

        _context.RefreshTokens.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(token, entity.ExpiresAtUtc);
    }

    public async Task<Result<RotatedRefreshToken>> RotateAsync(
        string presentedToken,
        string? ip = null,
        CancellationToken cancellationToken = default)
    {
        var now = _utcNow();
        var hash = Hash(presentedToken);

        var stored = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
            return Result.Failure<RotatedRefreshToken>(AuthErrors.InvalidRefreshToken);

        if (stored.IsRevoked)
        {
            var withinGrace = now - stored.RevokedAtUtc!.Value <= TimeSpan.FromSeconds(_options.GraceSeconds);
            if (withinGrace && stored.ReplacedByTokenId.HasValue)
                return await ReturnCurrentChildAsync(stored.FamilyId, ip, now, cancellationToken);

            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return Result.Failure<RotatedRefreshToken>(AuthErrors.InvalidRefreshToken);
        }

        if (stored.IsExpired(now))
        {
            if ((now - stored.ExpiresAtUtc).TotalSeconds <= _options.GraceSeconds)
                return await RotateAsync(stored, ip, now, cancellationToken);

            return Result.Failure<RotatedRefreshToken>(AuthErrors.InvalidRefreshToken);
        }

        if (stored.User is { IsDisabled: true })
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return Result.Failure<RotatedRefreshToken>(UserErrors.Disabled);
        }

        return await RotateAsync(stored, ip, now, cancellationToken);
    }

    public async Task RevokeFamilyAsync(string presentedToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(presentedToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is not null)
            await RevokeFamilyAsync(stored.FamilyId, _utcNow(), cancellationToken);
    }

    private async Task<Result<RotatedRefreshToken>> ReturnCurrentChildAsync(
        Guid familyId,
        string? ip,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var current = await _context.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null || current.IsExpired(now))
        {
            await RevokeFamilyAsync(familyId, now, cancellationToken);
            return Result.Failure<RotatedRefreshToken>(AuthErrors.InvalidRefreshToken);
        }

        var user = current.User ?? await _userManager.FindByIdAsync(current.UserId);
        current.LastUsedAtUtc = now;
        if (user is null)
            return Result.Failure<RotatedRefreshToken>(AuthErrors.InvalidRefreshToken);

        return Result.Succeed(new RotatedRefreshToken(user, string.Empty, current.ExpiresAtUtc));
    }

    private async Task<Result<RotatedRefreshToken>> RotateAsync(
        RefreshToken stored,
        string? ip,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (stored.User is { IsDisabled: true })
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return Result.Failure<RotatedRefreshToken>(UserErrors.Disabled);
        }

        stored.LastUsedAtUtc = now;
        var issued = await IssueAsync(await EnsureUserAsync(stored), ip, stored.FamilyId, cancellationToken);

        stored.RevokedAtUtc = now;
        stored.ReplacedByTokenId = await GetIdByHashAsync(issued.Token, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var user = stored.User!;
        return Result.Succeed(new RotatedRefreshToken(user, issued.Token, issued.ExpiresAtUtc));
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var active = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
            token.RevokedAtUtc = _utcNow();

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyList<SessionResponse>>> GetSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var now = _utcNow();

        var families = await _context.RefreshTokens
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.FamilyId)
            .Select(g => new
            {
                FamilyId = g.Key,
                CreatedAtUtc = g.Min(t => t.CreatedAtUtc),
                ExpiresAtUtc = g.Max(t => t.ExpiresAtUtc),
                LastUsedAtUtc = g.Max(t => t.LastUsedAtUtc ?? t.CreatedAtUtc),
                Active = g.Any(t => t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            })
            .ToListAsync(cancellationToken);

        var sessions = families
            .Where(f => f.Active)
            .OrderByDescending(f => f.LastUsedAtUtc)
            .Select(f => new SessionResponse(f.FamilyId, f.CreatedAtUtc, f.ExpiresAtUtc, f.LastUsedAtUtc))
            .ToList();

        return Result.Succeed<IReadOnlyList<SessionResponse>>(sessions);
    }

    public async Task<Result> RevokeFamilyForUserAsync(
        string userId,
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var owns = await _context.RefreshTokens
            .AnyAsync(t => t.UserId == userId && t.FamilyId == familyId, cancellationToken);

        if (!owns)
            return Result.Failure(AuthErrors.InvalidRefreshToken);

        await RevokeFamilyAsync(familyId, _utcNow(), cancellationToken);
        return Result.Succeed();
    }

    private async Task<ApplicationUser> EnsureUserAsync(RefreshToken stored)
        => stored.User ?? await _userManager.FindByIdAsync(stored.UserId)
           ?? throw new InvalidOperationException("Refresh token references a missing user");

    private async Task<Guid> GetIdByHashAsync(string plaintext, CancellationToken ct)
        => await _context.RefreshTokens
            .Where(t => t.TokenHash == Hash(plaintext))
            .Select(t => t.Id)
            .FirstAsync(ct);

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken ct)
    {
        var active = await _context.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ToListAsync(ct);

        foreach (var token in active)
            token.RevokedAtUtc = now;

        await _context.SaveChangesAsync(ct);
    }

    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
