using Ordering.Customer.Services;
using Ordering.Customer.Settings;

namespace Ordering.Customer;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingCustomerModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymobSettings>(configuration.GetSection(PaymobSettings.SectionName));
        services.AddHttpClient<IPaymobService, PaymobService>();
        services.AddSingleton<IPaymobCallbackVerifier, PaymobCallbackVerifier>();
        services.AddScoped<IPaymobCallbackService, PaymobCallbackService>();
        services.AddScoped<IOrdersService, OrdersService>();
        services.AddScoped<IReturnsService, ReturnsService>();
        return services;
    }
}
