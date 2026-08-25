namespace Shopping.Customer.Contracts;

public record CartProductResponse(
    int ProductId,
    string Name,
    string Sku,
    int Quantity,
    long PriceCents,
    int SalePercent,
    long FinalPriceCents,
    long LineTotalCents);

public record AppliedPromoResponse(int Id, string Code, int Percent, long? MaxSaleCents);

public record CartResponse(
    IReadOnlyList<CartProductResponse> Items,
    AppliedPromoResponse? PromoCode,
    long SubtotalCents,
    long DiscountCents,
    long TotalCents);

public record AddCartItemRequest(int ProductId, int Quantity = 1);

public record UpdateCartItemRequest(int ProductId, int Quantity);

public record ApplyPromoRequest(string Code);

public record WishListItemResponse(
    int ProductId,
    string Name,
    string Sku,
    string CategoryName,
    long PriceCents,
    int SalePercent,
    long FinalPriceCents,
    bool InStock);

public record ReviewRequest(byte Rating, string Text);

public record ReviewResponse(
    int Id,
    int ProductId,
    string ProductName,
    string ReviewerName,
    byte Rating,
    string Text,
    DateTime CreatedAt);
