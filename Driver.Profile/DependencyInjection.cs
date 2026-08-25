using Driver.Profile.Services;

namespace Driver.Profile;

public static class DependencyInjection
{
    public static IServiceCollection AddDriverProfileModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDriverProfileService, DriverProfileService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IDriverApplicationService, DriverApplicationService>();
        return services;
    }
}
