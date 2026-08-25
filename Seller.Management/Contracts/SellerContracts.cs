namespace Seller.Management.Contracts;

public record SellerManagementResponse(
    int StoreId,
    string Name,
    string OwnerId,
    string OwnerName,
    string OwnerEmail,
    string Slug,
    StoreStatus Status,
    int ProductsCount,
    DateTime CreatedAt);

public record UpdateSellerStatusRequest(StoreStatus Status, string? Reason = null);
