using Shopping.Customer.Contracts;

namespace Shopping.Customer.Services;

public interface ICartService
{
    Task<Result<CartResponse>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> UpdateItemAsync(string userId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> RemoveItemAsync(string userId, int productId, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> ClearAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> ApplyPromoAsync(string userId, ApplyPromoRequest request, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> RemovePromoAsync(string userId, CancellationToken cancellationToken = default);
}

public class CartService(AppDbContext context) : ICartService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<CartResponse>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetCartWithDetailsAsync(userId, trackChanges: false, cancellationToken);
        if (cart is null)
            return Result.Failure<CartResponse>(ShoppingErrors.Cart.Empty);

        return Result.Succeed(await BuildResponseAsync(cart.Id, cancellationToken));
    }

    public async Task<Result<CartResponse>> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        var product = await _context.Products.FindAsync([request.ProductId], cancellationToken);
        if (product is null || product.DeletedAt is not null)
            return Result.Failure<CartResponse>(CatalogErrors.Product.NotFound);

        var cartProduct = cart.CartProducts.FirstOrDefault(cp => cp.ProductId == product.Id);
        var newQuantity = (cartProduct?.Quantity ?? 0) + request.Quantity;

        if (product.Quantity < newQuantity)
            return Result.Failure<CartResponse>(CatalogErrors.Product.OutOfStock);

        if (cartProduct is null)
            cart.CartProducts.Add(new CartProduct { CartId = cart.Id, ProductId = product.Id, Quantity = newQuantity });
        else
            cartProduct.Quantity = newQuantity;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(await BuildResponseAsync(cart.Id, cancellationToken));
    }

    public async Task<Result<CartResponse>> UpdateItemAsync(string userId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetCartWithDetailsAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
            return Result.Failure<CartResponse>(ShoppingErrors.Cart.Empty);

        var cartProduct = cart.CartProducts.FirstOrDefault(cp => cp.ProductId == request.ProductId);
        if (cartProduct is null)
            return Result.Failure<CartResponse>(ShoppingErrors.Cart.ProductNotFound);

        if (cartProduct.Product!.Quantity < request.Quantity || cartProduct.Product.DeletedAt is not null)
            return Result.Failure<CartResponse>(CatalogErrors.Product.OutOfStock);

        cartProduct.Quantity = request.Quantity;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(await BuildResponseAsync(cart.Id, cancellationToken));
    }

    public async Task<Result<CartResponse>> RemoveItemAsync(string userId, int productId, CancellationToken cancellationToken = default)
    {
        var cart = await GetCartWithDetailsAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
            return Result.Failure<CartResponse>(ShoppingErrors.Cart.Empty);

        var cartProduct = cart.CartProducts.FirstOrDefault(cp => cp.ProductId == productId);
        if (cartProduct is null)
            return Result.Failure<CartResponse>(ShoppingErrors.Cart.ProductNotFound);

        _context.CartProducts.Remove(cartProduct);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(await BuildResponseAsync(cart.Id, cancellationToken));
    }

    public async Task<Result<CartResponse>> ClearAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetCartWithDetailsAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
            return Result.Failure<CartResponse>(ShoppingErrors.Cart.Empty);

        cart.PromoCodeId = null;
        _context.CartProducts.RemoveRange(cart.CartProducts);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(new CartResponse([], null, 0, 0, 0));
    }

    public async Task<Result<CartResponse>> ApplyPromoAsync(string userId, ApplyPromoRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetCartWithDetailsAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null || cart.CartProducts.Count == 0)
            return Result.Failure<CartResponse>(ShoppingErrors.Cart.Empty);

        var promoCode = await _context.PromoCodes
            .FirstOrDefaultAsync(pc => pc.Code == request.Code.ToUpperInvariant(), cancellationToken);

        if (promoCode is null || promoCode.DeletedAt is not null)
            return Result.Failure<CartResponse>(CatalogErrors.PromoCode.NotFound);

        if (!promoCode.Active)
            return Result.Failure<CartResponse>(CatalogErrors.PromoCode.Inactive);

        cart.PromoCodeId = promoCode.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(await BuildResponseAsync(cart.Id, cancellationToken));
    }

    public async Task<Result<CartResponse>> RemovePromoAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetCartWithDetailsAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
            return Result.Failure<CartResponse>(ShoppingErrors.Cart.Empty);

        cart.PromoCodeId = null;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(await BuildResponseAsync(cart.Id, cancellationToken));
    }

    private async Task<Cart?> GetCartWithDetailsAsync(string userId, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = _context.Carts
            .Include(c => c.PromoCode)
            .Include(c => c.CartProducts)
                .ThenInclude(cp => cp.Product)
                    .ThenInclude(p => p!.Media)
            .AsQueryable();

        if (!trackChanges)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    private async Task<Cart> GetOrCreateCartAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.CartProducts)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is not null) return cart;

        cart = new Cart { UserId = userId };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Carts
            .Include(c => c.CartProducts)
            .FirstAsync(c => c.Id == cart.Id, cancellationToken);
    }

    private async Task<CartResponse> BuildResponseAsync(int cartId, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .AsNoTracking()
            .Include(c => c.PromoCode)
            .Include(c => c.CartProducts)
                .ThenInclude(cp => cp.Product)
                    .ThenInclude(p => p!.Media)
            .FirstAsync(c => c.Id == cartId, cancellationToken);

        var productIds = cart.CartProducts
            .Where(cp => cp.Product != null && cp.Product.DeletedAt == null && cp.Quantity > 0)
            .Select(cp => cp.Product!.Id)
            .Distinct()
            .ToList();

        var offerDiscounts = await _context
            .LoadBestOfferDiscountByProductAsync(productIds, DateTime.UtcNow, cancellationToken);

        return ToResponse(cart, offerDiscounts);
    }

    private static CartResponse ToResponse(Cart cart, IReadOnlyDictionary<int, int> offerDiscounts)
    {
        var items = cart.CartProducts
            .Where(cp => cp.Product != null && cp.Product.DeletedAt == null && cp.Quantity > 0)
            .Select(cp =>
            {
                var product = cp.Product!;
                var (salePercent, finalPriceCents) = OfferPricing.EffectivePricing(product, offerDiscounts);
                return new CartProductResponse(
                    cp.ProductId,
                    product.Name,
                    product.Sku,
                    cp.Quantity,
                    product.PriceCents,
                    salePercent,
                    finalPriceCents,
                    finalPriceCents * cp.Quantity);
            })
            .ToList();

        var subtotal = items.Sum(i => i.LineTotalCents);

        long discount = 0;
        AppliedPromoResponse? promo = null;

        if (cart.PromoCode is { Active: true } promoCode)
        {
            promo = new AppliedPromoResponse(promoCode.Id, promoCode.Code, promoCode.Percent, promoCode.MaxSaleCents);
            discount = promoCode.MaxSaleCents is null
                ? subtotal * promoCode.Percent / 100
                : Math.Min(subtotal * promoCode.Percent / 100, promoCode.MaxSaleCents.Value);
        }

        return new CartResponse(items, promo, subtotal, discount, subtotal - discount);
    }
}
