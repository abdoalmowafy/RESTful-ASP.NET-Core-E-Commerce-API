using Seller.Profile.Services;

namespace Seller.Profile;

public static class DependencyInjection
{
    public static IServiceCollection AddSellerProfileModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISellerStoreService, SellerStoreService>();
        services.AddScoped<ISellerProductService, SellerProductService>();
        services.AddScoped<ISellerOrderService, SellerOrderService>();
        services.AddScoped<ISellerOfferService, SellerOfferService>();
        return services;
    }
}
