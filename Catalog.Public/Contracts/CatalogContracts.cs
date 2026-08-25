namespace Catalog.Public.Contracts;

public record CategoryResponse(int Id, string Name);

public record ProductBriefResponse(
    int Id,
    int StoreId,
    string StoreName,
    string Name,
    string Sku,
    string CategoryName,
    long PriceCents,
    int SalePercent,
    long FinalPriceCents,
    int WarrantyDays,
    int Quantity,
    string? ThumbnailUrl);

public record ProductDetailedResponse(
    int Id,
    int StoreId,
    string StoreName,
    string Name,
    string Sku,
    string Description,
    int CategoryId,
    string CategoryName,
    long PriceCents,
    int SalePercent,
    long FinalPriceCents,
    int WarrantyDays,
    int Quantity,
    bool InStock,
    double RatingAverage,
    int ReviewsCount,
    DateTime CreatedAt,
    IReadOnlyList<string> MediaUrls);

public record HomeResponse(
    IReadOnlyList<ProductBriefResponse> BestSellers,
    IReadOnlyList<ProductBriefResponse> TopDeals,
    IReadOnlyList<ProductBriefResponse> NewArrivals);

public record SearchRequest(
    string KeyWord,
    string CategoryName = "All",
    bool IncludeOutOfStock = false,
    int PageIndex = 1,
    int PageSize = 10);
