using Microsoft.Extensions.Options;
using Ordering.Customer.Contracts;
using Ordering.Customer.Services;
using Ordering.Customer.Settings;

namespace ECommerce.UnitTests.PaymobTests;

public class PaymobCallbackVerifierTests
{
    private static PaymobCallbackVerifier CreateSut(string secret = "test-hmac-secret")
        => new(Options.Create(new PaymobSettings { HmacSecret = secret }));

    private static PaymobTransaction SampleTransaction(
        long amountCents = 24_000,
        string createdAt = "2026-08-24T10:00:00.000Z",
        bool success = true)
        => new(
            Id: 5_551_234,
            Pending: false,
            Success: success,
            ErrorOccured: false,
            AmountCents: amountCents,
            CreatedAt: createdAt,
            Currency: "EGP",
            IntegrationId: 111_222,
            HasParentTransaction: false,
            Is3dSecure: true,
            IsAuth: false,
            IsCapture: false,
            IsRefunded: false,
            IsStandalonePayment: false,
            IsVoided: false,
            Owner: 42,
            Order: new PaymobCallbackOrder(987),
            SourceData: new PaymobSourceData("1234******5678", "MasterCardCredit", "CARD"));

    [Fact]
    public void Matching_hmac_is_accepted()
    {
        var transaction = SampleTransaction();
        var expected = Compute(transaction, "test-hmac-secret");

        Assert.True(CreateSut().IsValid(expected, transaction));
    }

    [Fact]
    public void Tampered_payload_is_rejected()
    {
        var transaction = SampleTransaction();
        var expected = Compute(transaction, "test-hmac-secret");
        var tampered = transaction with { AmountCents = 1 };

        Assert.False(CreateSut().IsValid(expected, tampered));
    }

    [Fact]
    public void Wrong_secret_is_rejected()
    {
        var transaction = SampleTransaction();
        var expected = Compute(transaction, "another-secret");

        Assert.False(CreateSut().IsValid(expected, transaction));
    }

    [Fact]
    public void Missing_signature_is_rejected_without_throwing()
    {
        Assert.False(CreateSut().IsValid(null, SampleTransaction()));
        Assert.False(CreateSut().IsValid(string.Empty, SampleTransaction()));
    }

    [Fact]
    public void Unconfigured_secret_never_validates()
    {
        var verifier = CreateSut(secret: "");
        var transaction = SampleTransaction();
        var expected = Compute(transaction, "test-hmac-secret");

        Assert.False(verifier.IsValid(expected, transaction));
    }

    private static string Compute(PaymobTransaction t, string secret)
    {
        var concatenated = string.Concat(
            t.AmountCents, t.CreatedAt, t.Currency,
            Bool(t.ErrorOccured), Bool(t.HasParentTransaction), t.Id, t.IntegrationId,
            Bool(t.Is3dSecure), Bool(t.IsAuth), Bool(t.IsCapture), Bool(t.IsRefunded),
            Bool(t.IsStandalonePayment), Bool(t.IsVoided),
            t.Order?.Id ?? 0, t.Owner, Bool(t.Pending),
            t.SourceData?.Pan ?? "", t.SourceData?.SubType ?? "", t.SourceData?.Type ?? "",
            Bool(t.Success));

        using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(concatenated))).ToLowerInvariant();
    }

    private static string Bool(bool v) => v ? "true" : "false";
}
