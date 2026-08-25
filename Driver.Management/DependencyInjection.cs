using Driver.Management.Services;

namespace Driver.Management;

public static class DependencyInjection
{
    public static IServiceCollection AddDriverManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDriverManagementService, DriverManagementService>();
        return services;
    }
}
