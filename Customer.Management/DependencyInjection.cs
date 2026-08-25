using Customer.Management.Services;

namespace Customer.Management;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomerManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICustomerManagementService, CustomerManagementService>();
        return services;
    }
}
