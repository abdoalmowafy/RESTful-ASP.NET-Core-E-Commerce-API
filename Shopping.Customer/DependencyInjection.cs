using Shopping.Customer.Services;

namespace Shopping.Customer;

public static class DependencyInjection
{
    public static IServiceCollection AddShoppingCustomerModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishListService, WishListService>();
        services.AddScoped<IReviewService, ReviewService>();
        return services;
    }
}
