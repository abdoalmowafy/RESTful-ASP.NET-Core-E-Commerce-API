using Notifications.Services;

namespace Notifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDeviceRegistryService, DeviceRegistryService>();
        return services;
    }
}
