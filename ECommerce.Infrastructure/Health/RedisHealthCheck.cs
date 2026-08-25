using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Health;

public class RedisHealthCheck(IConnectionMultiplexer multiplexer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await multiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy("Redis is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Redis is unreachable", ex);
        }
    }
}
