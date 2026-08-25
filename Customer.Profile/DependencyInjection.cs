using Customer.Profile.Services;

namespace Customer.Profile;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomerProfileModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<IAddressService, AddressService>();
        return services;
    }
}
