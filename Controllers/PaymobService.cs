using ECommerceAPI.Models;
using System.Text.Json;

namespace ECommerceAPI.Controllers
{
    public class PaymobService(HttpClient httpClient, IConfiguration configuration)
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfigurationSection _paymobConfig = configuration.GetRequiredSection("Paymob");
        private static readonly string BaseUrl = "https://accept.paymob.com/api/";

        private async Task<string> AuthenticateAsync()
        {
            var api_key = _paymobConfig["ApiKey"]!;
            var authRequest = new { api_key };

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/auth/tokens", authRequest);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<dynamic>(responseJson);

            return responseObject?.GetProperty("token")?.GetString() ?? throw new Exception("Authentication token not found.");
        }

        private async Task<int> RegisterOrderAsync(Order order, string authToken)
        {
            var orderRequest = new
            {
                AuthToken = authToken,
                order.DeliveryNeeded,
                AmountCents = order.TotalCents,
                order.Currency,
                Items = order.OrderProducts.Select(op => new
                {
                    op.Product.Name,
                    AmountCents = op.Product.PriceCents * (1 - op.SalePercent / 100),
                    op.Product.Quantity
                })
            };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/ecommerce/orders", orderRequest);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<dynamic>(responseJson);

            return responseObject?.GetProperty("id")?.GetString() ?? throw new Exception("Order registration failed.");
        }

        private async Task<string> RequestPaymentKeyAsync(Order order, int orderId, string authToken)
        {
            var integrationId = int.Parse(_paymobConfig["IntegrationId"]!);
            var address = order.Address;
            var user = order.User;

            var billingData = new
            {
                apartment = address.Apartment,
                email = user.Email,
                floor = address.Floor,
                first_name = user.Name!.Split().First(),
                street = address.Street,
                building = address.Building,
                phone_number = user.PhoneNumber,
                shipping_method = "PKG",
                postal_code = address.PostalCode,
                city = address.City,
                country = address.Country,
                last_name = user.Name.Split().Last(),
                state = address.State
            };

            var paymentKeyRequest = new
            {
                auth_token = authToken,
                amount_cents = order.TotalCents,
                expiration = 3600,
                order_id = orderId,
                billing_data = billingData,
                currency = order.Currency,
                integration_id = integrationId
            };

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/acceptance/payment_keys", paymentKeyRequest);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<dynamic>(responseJson);

            return responseObject?.GetProperty("token")?.GetString() ?? throw new Exception("Failed to retrieve payment key.");
        }

        private async Task<string> PayMobileWalletAsync(string paymentKey, string identifier)
        {
            var request = new
            {
                source = new
                {
                    identifier,
                    subtype = "WALLET"
                },
                payment_token = paymentKey
            };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/acceptance/payments/pay", request);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<dynamic>(responseJson);

            return responseObject?.GetProperty("redirection_url")?.GetString() ?? throw new Exception("Failed to retrieve payment url.");
        }

        public async Task<string> PayAsync(Order order, string? identifier)
        {
            var paymentMethod = order.PaymentMethod;
            var authToken = await AuthenticateAsync();
            var orderId = await RegisterOrderAsync(order, authToken);
            var paymentKey = await RequestPaymentKeyAsync(order, orderId, authToken);

            return paymentMethod switch
            {
                PaymentMethod.COD => throw new Exception("Invalid method!"),
                PaymentMethod.CreditCard => $"https://accept.paymobsolutions.com/api/acceptance/iframes/{_paymobConfig["Iframe1Id"]}?payment_token={paymentKey}",
                PaymentMethod.MobileWallet => await PayMobileWalletAsync(paymentKey, identifier ?? throw new Exception("Identifier is missing!")),
                _ => throw new NotImplementedException()
            };
        }
    }
}