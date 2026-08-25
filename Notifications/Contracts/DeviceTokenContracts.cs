namespace Notifications.Contracts;

public record RegisterDeviceRequest(string Token, DevicePlatform Platform, string? DeviceName = null);

public record RegisteredDeviceResponse(Guid Id, DevicePlatform Platform, string? DeviceName, DateTime LastRegisteredAtUtc);
