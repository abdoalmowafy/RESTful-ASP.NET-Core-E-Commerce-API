using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class DeviceToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public AppOwnerType OwnerType { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }
    public string Token { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public string? DeviceName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastRegisteredAtUtc { get; set; } = DateTime.UtcNow;

    public void MarkRegistered() => LastRegisteredAtUtc = DateTime.UtcNow;
}
