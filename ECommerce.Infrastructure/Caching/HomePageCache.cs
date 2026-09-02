using ECommerce.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Caching;

public sealed class HomePageCacheOptions
{
    public const string SectionName = "HomePageCache";
    public int TtlMinutes { get; set; } = 2;
}

/// <summary>
/// A shared, Redis-backed cache for the public catalog home page. Any module that
/// mutates products or offers that appear on the home page calls
/// <see cref="InvalidateHomeAsync"/> so the next read reflects the change.
/// The shared key lives in Redis, so catalog (reads) and seller/admin (writes)
/// all operate on the same entry regardless of which app instance served them.
/// </summary>
public sealed class HomePageCache(ICacheService cache, IOptions<HomePageCacheOptions> options)
{
    public const string Key = "catalog:home";

    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(Math.Max(1, options.Value.TtlMinutes));

    public async Task<T> GetOrCreateHomeAsync<T>(Func<Task<T>> factory) where T : class
    {
        var cached = await cache.GetAsync<T>(Key);
        if (cached is not null)
            return cached;

        var value = await factory();
        await cache.SetAsync(Key, value, _ttl);
        return value;
    }

    public Task InvalidateHomeAsync(CancellationToken cancellationToken = default)
        => cache.RemoveAsync(Key, cancellationToken);
}
