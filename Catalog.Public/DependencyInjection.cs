using Catalog.Public.Services;

namespace Catalog.Public;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogPublicModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICatalogService, CatalogService>();
        return services;
    }
}
