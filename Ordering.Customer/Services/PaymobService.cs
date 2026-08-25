using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ordering.Customer.Settings;

namespace Ordering.Customer.Services;

public interface IPaymobService
{
    Task<Result<string>> PayAsync(Order order, string identifier, CancellationToken cancellationToken = default);
}

public class PaymobService(HttpClient httpClient, IOptions<PaymobSettings> options) : IPaymobService
{
    private const string BaseUrl = "https://accept.paymob.com/api/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PaymobSettings _settings = options.Value;
    private readonly HttpClient _httpClient = httpClient;

    public async Task<Result<string>> PayAsync(Order order, string identifier, CancellationToken cancellationToken = default)
    {
        try
        {
            var authToken = await AuthenticateAsync(cancellationToken);
            var paymobOrderId = await RegisterOrderAsync(order, authToken, cancellationToken);

            order.PaymobOrderId = paymobOrderId;

            var paymentUrl = await CreatePaymentKeyAsync(
                order,
                authToken,
                paymobOrderId,
                identifier,
                cancellationToken);

            return Result.Succeed(paymentUrl);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<string>(OrderingErrors.Order.PaymentFailed);
        }
    }

    private async Task<string> AuthenticateAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{BaseUrl}auth/tokens",
            new { api_key = _settings.ApiKey },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, cancellationToken);
        return payload?.Token ?? throw new InvalidOperationException("Paymob authentication token missing");
    }

    private async Task<int> RegisterOrderAsync(Order order, string authToken, CancellationToken cancellationToken)
    {
        var request = new RegisterOrderRequest(
            AuthToken: authToken,
            DeliveryNeeded: order.DeliveryNeeded ? "PKG" : "NA",
            AmountCents: (int)order.TotalCents,
            Currency: order.Currency,
            MerchantOrderId: $"order-{order.Id}",
            Items: [.. order.OrderProducts.Select(op => new PaymobItem(
                Name: op.Product!.Name,
                AmountCents: (int)(op.Product.PriceCents * (100 - op.SalePercent) / 100),
                Description: op.Product.Sku,
                Quantity: op.Quantity))]);

        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}ecommerce/orders", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RegisterOrderResponse>(JsonOptions, cancellationToken);
        return payload?.Id ?? throw new InvalidOperationException("Paymob order registration failed");
    }

    private async Task<string> CreatePaymentKeyAsync(
        Order order,
        string authToken,
        int paymobOrderId,
        string identifier,
        CancellationToken cancellationToken)
    {
        var integrationId = order.PaymentMethod == PaymentMethod.MobileWallet
            ? _settings.IntegrationIdWallet
            : _settings.IntegrationIdCard;

        var user = order.User!;
        var address = order.Address!;
        var nameParts = user.FullName.Split(' ', 2);
        var request = new PaymentKeyRequest(
            AuthToken: authToken,
            AmountCents: (int)order.TotalCents,
            Expiration: 3600,
            OrderId: paymobOrderId,
            BillingData: new BillingData(
                FirstName: nameParts[0],
                LastName: nameParts.Length > 1 ? nameParts[^1] : nameParts[0],
                PhoneNumber: identifier ?? user.PhoneNumber ?? string.Empty,
                Email: user.Email ?? string.Empty,
                Apartment: address.Apartment,
                Floor: address.Floor,
                Building: address.Building,
                Street: address.Street,
                City: address.City,
                State: address.State,
                Country: address.Country,
                PostalCode: address.PostalCode,
                ShippingMethod: "PKG"),
            Currency: order.Currency,
            IntegrationId: int.Parse(integrationId));

        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}acceptance/payment_keys", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PaymentKeyResponse>(JsonOptions, cancellationToken);
        return $"https://accept.paymob.com/api/acceptance/iframes/{_settings.IframeId}?payment_token={payload?.Token}";
    }

    private sealed record AuthResponse([property: JsonPropertyName("token")] string Token);

    private sealed record RegisterOrderRequest(
        [property: JsonPropertyName("auth_token")] string AuthToken,
        [property: JsonPropertyName("delivery_needed")] string DeliveryNeeded,
        [property: JsonPropertyName("amount_cents")] int AmountCents,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("merchant_order_id")] string MerchantOrderId,
        [property: JsonPropertyName("items")] IReadOnlyList<PaymobItem> Items);

    private sealed record PaymobItem(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("amount_cents")] int AmountCents,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("quantity")] int Quantity);

    private sealed record RegisterOrderResponse([property: JsonPropertyName("id")] int Id);

    private sealed record PaymentKeyRequest(
        [property: JsonPropertyName("auth_token")] string AuthToken,
        [property: JsonPropertyName("amount_cents")] int AmountCents,
        [property: JsonPropertyName("expiration")] int Expiration,
        [property: JsonPropertyName("order_id")] int OrderId,
        [property: JsonPropertyName("billing_data")] BillingData BillingData,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("integration_id")] int IntegrationId);

    private sealed record BillingData(
        [property: JsonPropertyName("first_name")] string FirstName,
        [property: JsonPropertyName("last_name")] string LastName,
        [property: JsonPropertyName("phone_number")] string PhoneNumber,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("apartment")] string Apartment,
        [property: JsonPropertyName("floor")] string Floor,
        [property: JsonPropertyName("building")] string Building,
        [property: JsonPropertyName("street")] string Street,
        [property: JsonPropertyName("city")] string City,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("country")] string Country,
        [property: JsonPropertyName("postal_code")] string PostalCode,
        [property: JsonPropertyName("shipping_method")] string ShippingMethod);

    private sealed record PaymentKeyResponse([property: JsonPropertyName("token")] string Token);
}
