using Admin.Profile.Services;

namespace Admin.Profile;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminProfileModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAdminProfileService, AdminProfileService>();
        return services;
    }
}
