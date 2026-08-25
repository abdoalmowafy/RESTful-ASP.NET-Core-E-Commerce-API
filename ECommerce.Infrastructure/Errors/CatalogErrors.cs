namespace ECommerce.Infrastructure.Errors;

public static class CatalogErrors
{
    public static class Category
    {
        public static readonly Error NotFound = Error.NotFound("Category.NotFound", "Category not found");
        public static readonly Error NameDuplicated = Error.Conflict("Category.NameDuplicated", "Category name already exists");
        public static readonly Error HasProducts = Error.Conflict("Category.HasProducts", "Cannot delete a category that still has products");
    }

    public static class Product
    {
        public static readonly Error NotFound = Error.NotFound("Product.NotFound", "Product not found");
        public static readonly Error SkuDuplicated = Error.Conflict("Product.SkuDuplicated", "A product with the same SKU already exists");
        public static readonly Error OutOfStock = Error.Conflict("Product.OutOfStock", "Requested quantity exceeds available stock");
        public static readonly Error Deleted = Error.BadRequest("Product.Deleted", "Product is no longer available");
    }

    public static class PromoCode
    {
        public static readonly Error NotFound = Error.NotFound("PromoCode.NotFound", "Promo code not found");
        public static readonly Error CodeDuplicated = Error.Conflict("PromoCode.CodeDuplicated", "Promo code already exists");
        public static readonly Error Inactive = Error.BadRequest("PromoCode.Inactive", "Promo code is no longer active");
    }

    public static class StoreAddress
    {
        public static readonly Error NotFound = Error.NotFound("StoreAddress.NotFound", "Store address not found");
    }
}
