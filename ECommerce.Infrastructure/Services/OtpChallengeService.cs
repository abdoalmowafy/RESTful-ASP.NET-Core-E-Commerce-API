using System.Security.Cryptography;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;

namespace ECommerce.Infrastructure.Services;

public record IssuedOtp(string Code, DateTime ExpiresAtUtc);

public interface IOtpChallengeService
{
    Task<IssuedOtp> IssueAsync(OtpPurpose purpose, string target, CancellationToken cancellationToken = default);
    Task<bool> ValidateAndConsumeAsync(OtpPurpose purpose, string target, string code, CancellationToken cancellationToken = default);
}

public class OtpChallengeService(AppDbContext context, Func<DateTime>? utcNowFactory = null) : IOtpChallengeService
{
    public const int MaxFailedAttempts = 5;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _context = context;
    private readonly Func<DateTime> _utcNow = utcNowFactory ?? (() => DateTime.UtcNow);

    public async Task<IssuedOtp> IssueAsync(OtpPurpose purpose, string target, CancellationToken cancellationToken = default)
    {
        var normalized = target.Trim().ToLowerInvariant();
        var now = _utcNow();

        var stale = await _context.OtpCodes
            .Where(o => o.Purpose == purpose && o.Target == normalized && o.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

        _context.OtpCodes.RemoveRange(stale);

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        _context.OtpCodes.Add(new OtpCode
        {
            Purpose = purpose,
            Target = normalized,
            CodeHash = Hash(normalized, code),
            ExpiresAtUtc = now.Add(Lifetime)
        });

        await _context.SaveChangesAsync(cancellationToken);
        return new IssuedOtp(code, now.Add(Lifetime));
    }

    public async Task<bool> ValidateAndConsumeAsync(
        OtpPurpose purpose,
        string target,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalized = target.Trim().ToLowerInvariant();
        var now = _utcNow();

        var otp = await _context.OtpCodes
            .Where(o => o.Purpose == purpose && o.Target == normalized && o.ConsumedAtUtc == null)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || otp.IsExpired(now) || otp.FailedAttempts >= MaxFailedAttempts)
            return false;

        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(Hash(normalized, code)),
                Convert.FromHexString(otp.CodeHash)))
        {
            otp.FailedAttempts++;
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }

        otp.ConsumedAtUtc = now;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string Hash(string target, string code)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes($"{target}:{code}");
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
