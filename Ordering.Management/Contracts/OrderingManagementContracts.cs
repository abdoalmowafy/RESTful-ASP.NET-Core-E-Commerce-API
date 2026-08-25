namespace Ordering.Management.Contracts;

public record UpdateOrderStatusRequest(OrderStatus Status);

public record AssignTransporterRequest(string TransporterId);

public record ManagementOrderResponse(
    int Id,
    string CustomerName,
    string? TransporterName,
    long TotalCents,
    PaymentMethod PaymentMethod,
    bool DeliveryNeeded,
    OrderStatus Status,
    DateTime CreatedAt,
    int ItemsCount,
    string City);

public record ManagementReturnResponse(
    int Id,
    int OrderId,
    string ProductName,
    int Quantity,
    string Reason,
    ReturnStatus Status,
    DateTime CreatedAt,
    string RequestedByName,
    string? TransporterName);
