using Roles.Management.Services;

namespace Roles.Management;

public static class DependencyInjection
{
    public static IServiceCollection AddRolesManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IRolesManagementService, RolesManagementService>();
        return services;
    }
}
