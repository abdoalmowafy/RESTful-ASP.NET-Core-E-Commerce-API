namespace ECommerce.Infrastructure.Errors;

public static class ShoppingErrors
{
    public static class Cart
    {
        public static readonly Error Empty = Error.BadRequest("Cart.Empty", "Your cart is empty");
        public static readonly Error InvalidQuantity = Error.BadRequest("Cart.InvalidQuantity", "Quantity must be at least 1");
        public static readonly Error ProductNotFound = Error.NotFound("Cart.ProductNotFound", "Product not found in cart");
    }

    public static class WishList
    {
        public static readonly Error ProductNotFound = Error.NotFound("WishList.ProductNotFound", "Product not found");
    }

    public static class Review
    {
        public static readonly Error NotFound = Error.NotFound("Review.NotFound", "Review not found");
        public static readonly Error AlreadyReviewed = Error.Conflict("Review.AlreadyReviewed", "You have already reviewed this product");
        public static readonly Error NotPurchased = Error.Forbidden("Review.NotPurchased", "You can only review products you have purchased");
        public static readonly Error Forbidden = Error.Forbidden("Review.Forbidden", "You are not allowed to modify this review");
    }
}
