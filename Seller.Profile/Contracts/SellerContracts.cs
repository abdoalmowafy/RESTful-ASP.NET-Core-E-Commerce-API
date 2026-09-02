namespace Seller.Profile.Contracts;

public record StoreResponse(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    StoreStatus Status,
    string? RejectionReason);

public record UpsertStoreRequest(string Name, string? Description, string? LogoUrl);

public record SellerProductResponse(
    int Id,
    string Name,
    string Sku,
    string CategoryName,
    int Quantity,
    long PriceCents,
    int SalePercent,
    long FinalPriceCents,
    bool IsDeleted,
    DateTime CreatedAt);

public record SellerProductRequest(
    string Name,
    string Sku,
    string Description,
    int CategoryId,
    int Quantity,
    long PriceCents,
    int SalePercent,
    int WarrantyDays);

public record SellerStockRequest(int Quantity);

public record SellerOrderItemResponse(
    int OrderProductId,
    int OrderId,
    OrderStatus OrderStatus,
    string ProductName,
    int Quantity,
    long LineTotalCents,
    DateTime OrderedAt);

public record SellerOfferResponse(
    int Id,
    string Title,
    string? Description,
    int DiscountPercent,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<int> ProductIds);

public record UpsertOfferRequest(
    string Title,
    string? Description,
    int DiscountPercent,
    DateTime StartsAt,
    DateTime EndsAt,
    IReadOnlyList<int> ProductIds);

public record SetOfferActiveRequest(bool IsActive);
