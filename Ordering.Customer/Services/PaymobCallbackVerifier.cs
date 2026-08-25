using System.Security.Cryptography;
using System.Text;
using Ordering.Customer.Contracts;
using Ordering.Customer.Settings;

namespace Ordering.Customer.Services;

public interface IPaymobCallbackVerifier
{
    bool IsValid(string? receivedHmac, PaymobTransaction transaction);
}

public class PaymobCallbackVerifier(IOptions<PaymobSettings> options) : IPaymobCallbackVerifier
{
    private readonly string _hmacSecret = options.Value.HmacSecret;

    public bool IsValid(string? receivedHmac, PaymobTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(receivedHmac) || string.IsNullOrEmpty(_hmacSecret))
            return false;

        var concatenated = string.Concat(
            transaction.AmountCents,
            transaction.CreatedAt,
            transaction.Currency,
            Bool(transaction.ErrorOccured),
            Bool(transaction.HasParentTransaction),
            transaction.Id,
            transaction.IntegrationId,
            Bool(transaction.Is3dSecure),
            Bool(transaction.IsAuth),
            Bool(transaction.IsCapture),
            Bool(transaction.IsRefunded),
            Bool(transaction.IsStandalonePayment),
            Bool(transaction.IsVoided),
            transaction.Order?.Id ?? 0,
            transaction.Owner,
            Bool(transaction.Pending),
            transaction.SourceData?.Pan ?? string.Empty,
            transaction.SourceData?.SubType ?? string.Empty,
            transaction.SourceData?.Type ?? string.Empty,
            Bool(transaction.Success));

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_hmacSecret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated))).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(receivedHmac.ToLowerInvariant()));
    }

    private static string Bool(bool value) => value ? "true" : "false";
}
