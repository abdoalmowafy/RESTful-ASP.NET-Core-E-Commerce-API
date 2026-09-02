using Catalog.Public.Contracts;

namespace Catalog.Public.Services;

public interface ICatalogService
{
    Task<Result<IReadOnlyList<CategoryResponse>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Result<HomeResponse>> GetHomeAsync(CancellationToken cancellationToken = default);
    Task<Result<ProductDetailedResponse>> GetProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<ProductBriefResponse>>> SearchAsync(
        SearchRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}

public class CatalogService(AppDbContext context, HomePageCache homePageCache) : ICatalogService
{
    private static volatile bool _fullTextEnabled = true;

    private readonly AppDbContext _context = context;
    private readonly HomePageCache _homePageCache = homePageCache;

    public async Task<Result<IReadOnlyList<CategoryResponse>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<CategoryResponse>>(categories);
    }

    public async Task<Result<HomeResponse>> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        var home = await _homePageCache.GetOrCreateHomeAsync(() => BuildHomeAsync(cancellationToken));
        return Result.Succeed(home!);
    }

    private async Task<HomeResponse> BuildHomeAsync(CancellationToken cancellationToken)
    {
        var available = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Store)
            .Include(p => p.Media)
            .Where(p => p.DeletedAt == null && p.Quantity > 0 && p.Store!.DeletedAt == null && p.Store.Status == StoreStatus.Active);

        var bestSellers = await available
            .OrderByDescending(p => p.Views)
            .Take(25)
            .ToListAsync(cancellationToken);

        var topDeals = await available
            .OrderByDescending(p => p.SalePercent)
            .Take(25)
            .ToListAsync(cancellationToken);

        var newArrivals = await available
            .OrderByDescending(p => p.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        var allIds = bestSellers.Concat(topDeals).Concat(newArrivals)
            .Select(p => p.Id).Distinct().ToList();

        var offerDiscounts = await _context
            .LoadBestOfferDiscountByProductAsync(allIds, DateTime.UtcNow, cancellationToken);

        return new HomeResponse(
            [.. bestSellers.Select(p => ToDetailedBrief(p, offerDiscounts))],
            [.. topDeals.Select(p => ToDetailedBrief(p, offerDiscounts))],
            [.. newArrivals.Select(p => ToDetailedBrief(p, offerDiscounts))]);
    }

    public async Task<Result<ProductDetailedResponse>> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Store)
            .Include(p => p.Media)
            .Include(p => p.Reviews.Where(r => r.DeletedAt == null))
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null ||
            product.DeletedAt is not null ||
            product.Store is null ||
            product.Store.DeletedAt is not null ||
            product.Store.Status != StoreStatus.Active)
            return Result.Failure<ProductDetailedResponse>(CatalogErrors.Product.NotFound);

        product.Views++;
        await _context.SaveChangesAsync(cancellationToken);

        var offerDiscounts = await _context
            .LoadBestOfferDiscountByProductAsync([product.Id], DateTime.UtcNow, cancellationToken);

        var (effectiveSalePercent, effectiveFinalPriceCents) = EffectivePricing(product, offerDiscounts);

        var response = new ProductDetailedResponse(
            product.Id,
            product.StoreId,
            product.Store.Name,
            product.Name,
            product.Sku,
            product.Description,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.PriceCents,
            effectiveSalePercent,
            effectiveFinalPriceCents,
            product.WarrantyDays,
            product.Quantity,
            product.Quantity > 0,
            Math.Round(product.Reviews.Count == 0 ? 0 : product.Reviews.Average(r => (double)r.Rating), 2),
            product.Reviews.Count,
            product.CreatedAt,
            [.. product.Media.Select(m => m.Url)]);

        return Result.Succeed(response);
    }

    public async Task<Result<PaginatedList<ProductBriefResponse>>> SearchAsync(
        SearchRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        Category? category = null;
        if (!string.Equals(request.CategoryName, "All", StringComparison.OrdinalIgnoreCase))
        {
            category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == request.CategoryName, cancellationToken);

            if (category is null)
                return Result.Failure<PaginatedList<ProductBriefResponse>>(CatalogErrors.Category.NotFound);
        }

        var products = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Store)
            .Where(p => p.DeletedAt == null && p.Store!.DeletedAt == null && p.Store.Status == StoreStatus.Active);

        var keyword = request.KeyWord?.Trim();
        var hasKeyword = !string.IsNullOrWhiteSpace(keyword);

        if (hasKeyword && _fullTextEnabled)
            products = WithFullText(products, keyword!);

        if (hasKeyword && !_fullTextEnabled)
            products = WithLikeSearch(products, keyword!);

        if (category is not null)
            products = products.Where(p => p.CategoryId == category.Id);

        if (!request.IncludeOutOfStock)
            products = products.Where(p => p.Quantity > 0);

        if (user.Identity?.IsAuthenticated == true && hasKeyword)
        {
            _context.Searches.Add(new Search
            {
                UserId = user.GetUserId(),
                CategoryId = category?.Id,
                KeyWord = keyword!
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        var ordered = products
            .OrderByDescending(p => p.Views)
            .ThenBy(p => p.Name);

        PaginatedList<Product> page;
        try
        {
            page = await PaginatedList<Product>.CreateAsync(ordered, request.PageIndex, request.PageSize, cancellationToken);
        }
        catch (Exception) when (hasKeyword && _fullTextEnabled)
        {
            _fullTextEnabled = false;

            var fallback = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Store)
                .Where(p => p.DeletedAt == null && p.Store!.DeletedAt == null && p.Store.Status == StoreStatus.Active);

            fallback = WithLikeSearch(fallback, keyword!);

            if (category is not null)
                fallback = fallback.Where(p => p.CategoryId == category.Id);

            if (!request.IncludeOutOfStock)
                fallback = fallback.Where(p => p.Quantity > 0);

            page = await PaginatedList<Product>.CreateAsync(
                fallback.OrderByDescending(p => p.Views).ThenBy(p => p.Name),
                request.PageIndex, request.PageSize, cancellationToken);
        }


        // Stage 2: pg_trgm fuzzy match (typos) when the FTS stage found nothing.
        if (page.Items.Count == 0 && hasKeyword && _fullTextEnabled)
        {
            var fuzzy = BuildTrigramStage(keyword!)
                .Where(p => p.DeletedAt == null && p.Quantity > 0 &&
                            p.Store!.DeletedAt == null && p.Store!.Status == StoreStatus.Active);

            if (category is not null)
                fuzzy = fuzzy.Where(p => p.CategoryId == category.Id);

            page = await PaginatedList<Product>.CreateAsync(
                fuzzy.OrderByDescending(p => p.Views).ThenBy(p => p.Name),
                request.PageIndex, request.PageSize, cancellationToken);
        }

        var offerDiscounts = await _context
            .LoadBestOfferDiscountByProductAsync(page.Items.Select(p => p.Id).ToList(), DateTime.UtcNow, cancellationToken);

        var mapped = page.Items.Select(p => ToBrief(p, offerDiscounts))
            .ToList();

        return Result.Succeed<PaginatedList<ProductBriefResponse>>(
            new PaginatedList<ProductBriefResponse>(mapped, page.PageNumber, page.TotalCount, request.PageSize));
    }

    /// <summary>
    /// Stage 2: pg_trgm similarity (% operator + ILIKE), ordered by best similarity.
    /// Uses raw SQL because similarity ordering is a PostgreSQL-specific construct.
    /// </summary>
    private IQueryable<Product> BuildTrigramStage(string keyword)
    {
        var like = $"%{keyword.EscapeLikePattern()}%";

        return _context.Products
            .FromSqlInterpolated($"""
                SELECT p.*, p.xmin AS "xmin"
                FROM "Products" AS p
                WHERE p."DeletedAt" IS NULL
                  AND (
                        p."Name" % {keyword}
                     OR p."Description" % {keyword}
                     OR p."Name" ILIKE {like}
                     OR p."Description" ILIKE {like}
                  )
                ORDER BY GREATEST(similarity(p."Name", {keyword}), similarity(p."Description", {keyword})) DESC
                """)
            .Include(p => p.Category)
            .Include(p => p.Store)
            .Include(p => p.Media);
    }

    private static IQueryable<Product> WithLikeSearch(IQueryable<Product> products, string keyword)
    {
        var pattern = $"%{keyword.EscapeLikePattern()}%";

        return products.Where(p =>
            EF.Functions.ILike(p.Name!, pattern) ||
            EF.Functions.ILike(p.Description!, pattern) ||
            EF.Functions.ILike(p.Sku!, pattern));
    }

    /// <summary>
    /// Stage 1: Full-Text over the accent-stripped Name + Description
    /// (matches the f_unaccent-based GIN expression index).
    /// </summary>
    private static IQueryable<Product> WithFullText(IQueryable<Product> products, string keyword)
    {
        var normalized = Normalize(keyword);
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length >= 2)
            .Select(w => w.Replace("'", string.Empty).Replace("\\", string.Empty)
                          .Replace(":", string.Empty).Replace("|", string.Empty))
            .ToArray();

        if (words.Length == 0)
            return WithLikeSearch(products, keyword);

        var tsQuery = string.Join(" | ", words.Select(w => $"{w}:*"));

        return products.Where(p =>
            EF.Functions.ToTsVector("english", PgFunctions.Unaccent(p.Name) + " " + PgFunctions.Unaccent(p.Description))
                .Matches(EF.Functions.ToTsQuery("english", tsQuery)));
    }

    private static string Normalize(string value)
    {
        var formD = value.Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(formD.Length);

        foreach (var c in formD)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                builder.Append(c);

        return builder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static ProductBriefResponse ToDetailedBrief(Product p, IReadOnlyDictionary<int, int> offerDiscounts)
    {
        var (salePercent, finalPriceCents) = EffectivePricing(p, offerDiscounts);
        return new ProductBriefResponse(
            p.Id,
            p.StoreId,
            p.Store?.Name ?? string.Empty,
            p.Name,
            p.Sku,
            p.Category?.Name ?? string.Empty,
            p.PriceCents,
            salePercent,
            finalPriceCents,
            p.WarrantyDays,
            p.Quantity,
            p.Media.OrderBy(m => m.Id).Select(m => m.Url).FirstOrDefault());
    }

    private static ProductBriefResponse ToBrief(Product p, IReadOnlyDictionary<int, int> offerDiscounts)
    {
        var (salePercent, finalPriceCents) = EffectivePricing(p, offerDiscounts);
        return new ProductBriefResponse(
            p.Id,
            p.StoreId,
            p.Store?.Name ?? string.Empty,
            p.Name,
            p.Sku,
            p.Category?.Name ?? string.Empty,
            p.PriceCents,
            salePercent,
            finalPriceCents,
            p.WarrantyDays,
            p.Quantity,
            p.Media.OrderBy(m => m.Id).Select(m => m.Url).FirstOrDefault());
    }

    private static (int SalePercent, long FinalPriceCents) EffectivePricing(
        Product p,
        IReadOnlyDictionary<int, int> offerDiscounts)
    {
        var effectivePercent = OfferPricing.EffectiveSalePercent(
            basePercent: p.SalePercent,
            offerPercent: offerDiscounts.GetValueOrDefault(p.Id));
        var finalCents = OfferPricing.EffectiveFinalPriceCents(p.PriceCents, effectivePercent);
        return (effectivePercent, finalCents);
    }
}
