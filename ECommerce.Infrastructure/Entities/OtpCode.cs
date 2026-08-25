using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class OtpCode
{
    public int Id { get; set; }
    public OtpPurpose Purpose { get; set; }
    public string Target { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public int FailedAttempts { get; set; }

    public bool IsExpired(DateTime now) => ExpiresAtUtc <= now;
    public bool IsConsumed => ConsumedAtUtc is not null;
}
