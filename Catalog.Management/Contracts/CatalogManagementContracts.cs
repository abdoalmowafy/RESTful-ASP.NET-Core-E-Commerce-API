namespace Catalog.Management.Contracts;

public record CategoryRequest(string Name);

public record CategoryManagementResponse(int Id, string Name, int ProductsCount);

public record ProductManagementResponse(
    int Id,
    string Name,
    string Sku,
    string Description,
    int CategoryId,
    string CategoryName,
    int Quantity,
    long Views,
    long PriceCents,
    int SalePercent,
    long FinalPriceCents,
    int WarrantyDays,
    bool IsDeleted,
    DateTime CreatedAt,
    IReadOnlyList<string> MediaUrls);

public record ProductRequest(
    string Name,
    string Sku,
    string Description,
    int CategoryId,
    int Quantity,
    long PriceCents,
    int SalePercent,
    int WarrantyDays);

public record StockRequest(int Quantity);

public record PromoCodeRequest(
    string Code,
    string Description,
    int Percent,
    long? MaxSaleCents,
    bool Active);

public record PromoCodeManagementResponse(
    int Id,
    string Code,
    string Description,
    int Percent,
    long? MaxSaleCents,
    bool Active);

public record StatusRequest(bool Active);

public record StoreAddressRequest(
    string Apartment,
    string Floor,
    string Building,
    string Street,
    string City,
    string State,
    string Country,
    string PostalCode);

public record AddressManagementResponse(
    int Id,
    string Apartment,
    string Floor,
    string Building,
    string Street,
    string City,
    string State,
    string Country,
    string PostalCode);
