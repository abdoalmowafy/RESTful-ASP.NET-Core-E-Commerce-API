using Shopping.Customer.Contracts;

namespace Shopping.Customer.Services;

public interface IWishListService
{
    Task<Result<IReadOnlyList<WishListItemResponse>>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<WishListItemResponse>>> AddAsync(string userId, int productId, CancellationToken cancellationToken = default);
    Task<Result> RemoveAsync(string userId, int productId, CancellationToken cancellationToken = default);
}

public class WishListService(AppDbContext context) : IWishListService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<IReadOnlyList<WishListItemResponse>>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.WishList)
            .Include(p => p.Category)
            .Select(p => ToItem(p))
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<WishListItemResponse>>(items);
    }

    public async Task<Result<IReadOnlyList<WishListItemResponse>>> AddAsync(string userId, int productId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.WishList)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result.Failure<IReadOnlyList<WishListItemResponse>>(UserErrors.NotFound);

        var product = await _context.Products.FindAsync([productId], cancellationToken);
        if (product is null || product.DeletedAt is not null)
            return Result.Failure<IReadOnlyList<WishListItemResponse>>(ShoppingErrors.WishList.ProductNotFound);

        if (user.WishList.All(p => p.Id != productId))
        {
            user.WishList.Add(product);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<Result> RemoveAsync(string userId, int productId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.WishList)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var product = user.WishList.FirstOrDefault(p => p.Id == productId);
        if (product is not null)
        {
            user.WishList.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Succeed();
    }

    private static WishListItemResponse ToItem(Product p)
        => new(
            p.Id,
            p.Name,
            p.Sku,
            p.Category?.Name ?? string.Empty,
            p.PriceCents,
            p.SalePercent,
            p.FinalPriceCents,
            p.Quantity > 0);
}
