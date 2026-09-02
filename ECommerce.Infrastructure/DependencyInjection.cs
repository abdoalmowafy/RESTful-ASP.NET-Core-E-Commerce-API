using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Health;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Npgsql;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(180);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                }));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();

        services.Configure<HomePageCacheOptions>(configuration.GetSection(HomePageCacheOptions.SectionName));
        services.AddScoped<HomePageCache>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<CacheService>>();
            var connectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogWarning("Redis connection string is missing. Defaulting to localhost:6379");
                connectionString = "localhost:6379";
            }

            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        services.AddStackExchangeRedisCache(options => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                options.ConnectionMultiplexerFactory =
                    () => Task.FromResult(sp.GetRequiredService<IConnectionMultiplexer>());
            });

        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IDriverLocationService, DriverLocationService>();
        services.AddScoped<IOtpChallengeService, OtpChallengeService>();

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddScoped<IFileStorage, LocalFileStorage>();

        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<INotificationDelivery, NotificationDeliveryService>();

        services.AddSignalR();
        services.Configure<FcmSettings>(configuration.GetSection(FcmSettings.SectionName));
        services.Configure<DataRetentionOptions>(configuration.GetSection(DataRetentionOptions.SectionName));
        services.AddHostedService<DataRetentionBackgroundService>();
        services.AddSingleton<IPushSender, FirebasePushSender>();
        services.AddScoped<IDeviceTokenService, DeviceTokenService>();
        services.AddScoped<IOrderTrackingNotifier, TrackingNotificationDispatcher>();

        services.AddSingleton<NpgsqlHealthCheck>(sp =>
            new NpgsqlHealthCheck(configuration.GetConnectionString("DefaultConnection") ?? string.Empty));

        services.AddHealthChecks()
            .AddCheck<NpgsqlHealthCheck>("postgres")
            .AddCheck<RedisHealthCheck>("redis");

        return services;
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
        => app;
}
