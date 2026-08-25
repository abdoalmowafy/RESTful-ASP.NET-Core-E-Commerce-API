using System.Text.Json.Serialization;

namespace Ordering.Customer.Contracts;

public record PaymobCallbackPayload(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("obj")] PaymobTransaction? Obj);

public record PaymobTransaction(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("pending")] bool Pending,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error_occured")] bool ErrorOccured,
    [property: JsonPropertyName("amount_cents")] long AmountCents,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("integration_id")] long IntegrationId,
    [property: JsonPropertyName("has_parent_transaction")] bool HasParentTransaction,
    [property: JsonPropertyName("is_3d_secure")] bool Is3dSecure,
    [property: JsonPropertyName("is_auth")] bool IsAuth,
    [property: JsonPropertyName("is_capture")] bool IsCapture,
    [property: JsonPropertyName("is_refunded")] bool IsRefunded,
    [property: JsonPropertyName("is_standalone_payment")] bool IsStandalonePayment,
    [property: JsonPropertyName("is_voided")] bool IsVoided,
    [property: JsonPropertyName("owner")] long Owner,
    [property: JsonPropertyName("order")] PaymobCallbackOrder? Order,
    [property: JsonPropertyName("source_data")] PaymobSourceData? SourceData);

public record PaymobCallbackOrder([property: JsonPropertyName("id")] int Id);

public record PaymobSourceData(
    [property: JsonPropertyName("pan")] string? Pan,
    [property: JsonPropertyName("sub_type")] string? SubType,
    [property: JsonPropertyName("type")] string? Type);

public record OrderTimelineItem(OrderStatus Status, DateTime OccurredAt, string? Note);
