using Seller.Management.Contracts;
using Seller.Management.Services;

namespace Seller.Management;

public static class DependencyInjection
{
    public static IServiceCollection AddSellerManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISellerManagementService, SellerManagementService>();
        return services;
    }
}
