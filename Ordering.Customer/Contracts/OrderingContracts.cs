namespace Ordering.Customer.Contracts;

public record CheckoutRequest(
    int AddressId,
    bool DeliveryNeeded,
    PaymentMethod PaymentMethod,
    string? Identifier = null);

public record CheckoutResponse(OrderResponse? Order, string? PaymentUrl);

public record OrderProductResponse(
    int ProductId,
    string ProductName,
    string Sku,
    long PriceCents,
    int SalePercent,
    long FinalPriceCents,
    int Quantity,
    int WarrantyDays);

public record OrderResponse(
    int Id,
    long TotalCents,
    string Currency,
    PaymentMethod PaymentMethod,
    bool DeliveryNeeded,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? DeliveredAt,
    AddressResponse Address,
    IReadOnlyList<OrderProductResponse> Items);

public record AddressResponse(
    int Id,
    string Apartment,
    string Floor,
    string Building,
    string Street,
    string City,
    string State,
    string Country,
    string PostalCode);

public record ReturnRequestResponse(
    int Id,
    int OrderId,
    int OrderProductId,
    string ProductName,
    int Quantity,
    string Reason,
    ReturnStatus Status,
    DateTime CreatedAt);
