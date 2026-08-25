using Shopping.Customer.Contracts;

namespace Shopping.Customer.Services;

public interface IReviewService
{
    Task<Result<IReadOnlyList<ReviewResponse>>> GetForProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<Result<ReviewResponse>> CreateAsync(string userId, int productId, ReviewRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReviewResponse>> UpdateAsync(string userId, int reviewId, ReviewRequest request, bool isStaff, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string userId, int reviewId, bool isStaff, CancellationToken cancellationToken = default);
}

public class ReviewService(AppDbContext context) : IReviewService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<IReadOnlyList<ReviewResponse>>> GetForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId && r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .Select(ToResponse())
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<ReviewResponse>>(reviews);
    }

    public async Task<Result<ReviewResponse>> CreateAsync(string userId, int productId, ReviewRequest request, CancellationToken cancellationToken = default)
    {
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId && p.DeletedAt == null, cancellationToken);
        if (!productExists)
            return Result.Failure<ReviewResponse>(CatalogErrors.Product.NotFound);

        var purchased = await HasPurchasedAsync(userId, productId, cancellationToken);
        if (!purchased)
            return Result.Failure<ReviewResponse>(ShoppingErrors.Review.NotPurchased);

        if (await _context.Reviews.AnyAsync(
                r => r.ProductId == productId &&
                     r.ReviewerId == userId &&
                     r.DeletedAt == null,
                cancellationToken))
            return Result.Failure<ReviewResponse>(ShoppingErrors.Review.AlreadyReviewed);

        var review = new Review
        {
            ReviewerId = userId,
            ProductId = productId,
            Rating = request.Rating,
            Text = request.Text
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(await LoadResponseAsync(review.Id, cancellationToken));
    }

    public async Task<Result<ReviewResponse>> UpdateAsync(string userId, int reviewId, ReviewRequest request, bool isStaff, CancellationToken cancellationToken = default)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.DeletedAt == null, cancellationToken);
        if (review is null)
            return Result.Failure<ReviewResponse>(ShoppingErrors.Review.NotFound);

        if (!isStaff && review.ReviewerId != userId)
            return Result.Failure<ReviewResponse>(ShoppingErrors.Review.Forbidden);

        review.Rating = request.Rating;
        review.Text = request.Text;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(await LoadResponseAsync(review.Id, cancellationToken));
    }

    public async Task<Result> DeleteAsync(string userId, int reviewId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.DeletedAt == null, cancellationToken);
        if (review is null)
            return Result.Failure(ShoppingErrors.Review.NotFound);

        if (!isStaff && review.ReviewerId != userId)
            return Result.Failure(ShoppingErrors.Review.Forbidden);

        review.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed();
    }

    private async Task<bool> HasPurchasedAsync(string userId, int productId, CancellationToken cancellationToken)
        => await _context.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered && o.DeletedAt == null)
            .SelectMany(o => o.OrderProducts)
            .AnyAsync(op => op.ProductId == productId, cancellationToken);

    private async Task<ReviewResponse> LoadResponseAsync(int reviewId, CancellationToken cancellationToken)
    {
        var response = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Include(r => r.Product)
            .Where(r => r.Id == reviewId)
            .Select(ToResponse())
            .FirstAsync(cancellationToken);

        return response;
    }

    private static System.Linq.Expressions.Expression<Func<Review, ReviewResponse>> ToResponse()
        => r => new ReviewResponse(
            r.Id,
            r.ProductId,
            r.Product!.Name,
            $"{r.Reviewer!.FirstName} {r.Reviewer.LastName}".Trim(),
            r.Rating,
            r.Text,
            r.CreatedAt);
}
