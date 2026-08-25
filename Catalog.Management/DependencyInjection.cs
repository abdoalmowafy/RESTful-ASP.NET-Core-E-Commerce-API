using Catalog.Management.Services;

namespace Catalog.Management;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProductManagementService, ProductManagementService>();
        services.AddScoped<ICategoryManagementService, CategoryManagementService>();
        services.AddScoped<IPromoCodeManagementService, PromoCodeManagementService>();
        services.AddScoped<IStoreAddressManagementService, StoreAddressManagementService>();
        return services;
    }
}
