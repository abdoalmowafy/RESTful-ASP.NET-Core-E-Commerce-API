using Admin.Management.Services;

namespace Admin.Management;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IStoreManagementService, StoreManagementService>();
        services.AddScoped<IAdminAccountsService, AdminAccountsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
