using Ordering.Management.Services;

namespace Ordering.Management;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOrderManagementService, OrderManagementService>();
        services.AddScoped<IReturnManagementService, ReturnManagementService>();
        return services;
    }
}
