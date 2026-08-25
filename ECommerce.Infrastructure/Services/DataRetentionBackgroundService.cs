using ECommerce.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

public sealed class DataRetentionOptions
{
    public const string SectionName = "DataRetention";
    public bool Enabled { get; set; } = true;
    public int FirstRunDelayMinutes { get; set; } = 5;
    public int IntervalHours { get; set; } = 24;
    public int KeepConsumedOtpDays { get; set; } = 1;
    public int KeepExpiredRefreshTokenDays { get; set; } = 30;
    public int KeepRevokedRefreshTokenDays { get; set; } = 90;
    public int KeepStaleDeviceDays { get; set; } = 90;
    public int KeepSearchDays { get; set; } = 180;
}

/// <summary>
/// Purges expired/rotated-out rows so operational tables don't grow forever.
/// Runs on a timer inside the API host; safe to disable via config.
/// </summary>
public sealed class DataRetentionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<DataRetentionOptions> options,
    ILogger<DataRetentionBackgroundService> logger) : BackgroundService
{
    private readonly DataRetentionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(_options.FirstRunDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunCleanupAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(_options.IntervalHours), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    internal async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        try
        {
            var otpCutoff = now.AddDays(-_options.KeepConsumedOtpDays);
            var otpDeleted = await db.OtpCodes
                .Where(o => o.ConsumedAtUtc != null && o.ConsumedAtUtc < otpCutoff
                         || o.ExpiresAtUtc < otpCutoff)
                .ExecuteDeleteAsync(cancellationToken);

            var refreshExpiredCutoff = now.AddDays(-_options.KeepExpiredRefreshTokenDays);
            var refreshRevokedCutoff = now.AddDays(-_options.KeepRevokedRefreshTokenDays);
            var refreshDeleted = await db.RefreshTokens
                .Where(t => t.ExpiresAtUtc < refreshExpiredCutoff
                         || (t.RevokedAtUtc != null && t.RevokedAtUtc < refreshRevokedCutoff))
                .ExecuteDeleteAsync(cancellationToken);

            var deviceCutoff = now.AddDays(-_options.KeepStaleDeviceDays);
            var devicesDeleted = await db.DeviceTokens
                .Where(t => t.LastRegisteredAtUtc < deviceCutoff)
                .ExecuteDeleteAsync(cancellationToken);

            var searchCutoff = now.AddDays(-_options.KeepSearchDays);
            var searchesDeleted = await db.Searches
                .Where(s => s.SearchedAt < searchCutoff)
                .ExecuteDeleteAsync(cancellationToken);

            logger.LogInformation(
                "Data retention pass: {Otp} OTP codes, {Refresh} refresh tokens, {Devices} stale devices, {Searches} searches removed",
                otpDeleted, refreshDeleted, devicesDeleted, searchesDeleted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data retention pass failed");
        }
    }
}
