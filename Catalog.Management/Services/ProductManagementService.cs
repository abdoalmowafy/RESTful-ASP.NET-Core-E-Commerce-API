using Catalog.Management.Contracts;

namespace Catalog.Management.Services;

public interface IProductManagementService
{
    Task<Result<PaginatedList<ProductManagementResponse>>> GetAsync(int pageIndex, int pageSize, bool includeDeleted, CancellationToken cancellationToken = default);
    Task<Result<ProductManagementResponse>> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ProductManagementResponse>> CreateAsync(ProductRequest request, IList<IFormFile> media, CancellationToken cancellationToken = default);
    Task<Result<ProductManagementResponse>> UpdateAsync(int id, ProductRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetStockAsync(int id, StockRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}

public class ProductManagementService(AppDbContext context, IFileStorage fileStorage, HomePageCache homePageCache) : IProductManagementService
{
    private static readonly string[] ProductAllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm", ".mov"];

    private readonly AppDbContext _context = context;
    private readonly HomePageCache _homePageCache = homePageCache;
    
    public async Task<Result<PaginatedList<ProductManagementResponse>>> GetAsync(int pageIndex, int pageSize, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Media)
            .OrderByDescending(p => p.CreatedAt);

        if (!includeDeleted)
            query = (IOrderedQueryable<Product>)query.Where(p => p.DeletedAt == null);

        var page = await PaginatedList<Product>.CreateAsync(query, pageIndex, pageSize, cancellationToken);

        return Result.Succeed(new PaginatedList<ProductManagementResponse>(
            page.Items.Select(ToResponse).ToList(),
            page.PageNumber,
            page.TotalCount,
            page.TotalPages));
    }

    public async Task<Result<ProductManagementResponse>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await FindAsync(id, trackChanges: false, cancellationToken);
        return product is null
            ? Result.Failure<ProductManagementResponse>(CatalogErrors.Product.NotFound)
            : Result.Succeed(ToResponse(product));
    }

    public async Task<Result<ProductManagementResponse>> CreateAsync(ProductRequest request, IList<IFormFile> media, CancellationToken cancellationToken = default)
    {
        if (!await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
            return Result.Failure<ProductManagementResponse>(CatalogErrors.Category.NotFound);

        if (await _context.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
            return Result.Failure<ProductManagementResponse>(CatalogErrors.Product.SkuDuplicated);

        var product = new Product
        {
            Name = request.Name,
            Sku = request.Sku.ToUpperInvariant(),
            Description = request.Description,
            CategoryId = request.CategoryId,
            Quantity = request.Quantity,
            PriceCents = request.PriceCents,
            SalePercent = request.SalePercent,
            WarrantyDays = request.WarrantyDays
        };

        var savedFiles = await fileStorage.SaveAllAsync(media, "media/products", 10 * 1024 * 1024, ProductAllowedExtensions, cancellationToken);
        foreach (var f in savedFiles)
            product.Media.Add(new ProductMedia { Url = f.Url });

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        await _homePageCache.InvalidateHomeAsync(cancellationToken);

        product.Category = await _context.Categories.FindAsync([product.CategoryId], cancellationToken);
        return Result.Succeed(ToResponse(product));
    }

    public async Task<Result<ProductManagementResponse>> UpdateAsync(int id, ProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await FindAsync(id, trackChanges: true, cancellationToken);
        if (product is null)
            return Result.Failure<ProductManagementResponse>(CatalogErrors.Product.NotFound);

        if (!await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
            return Result.Failure<ProductManagementResponse>(CatalogErrors.Category.NotFound);

        if (await _context.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != id, cancellationToken))
            return Result.Failure<ProductManagementResponse>(CatalogErrors.Product.SkuDuplicated);

        product.Name = request.Name;
        product.Sku = request.Sku.ToUpperInvariant();
        product.Description = request.Description;
        product.CategoryId = request.CategoryId;
        product.Quantity = request.Quantity;
        product.PriceCents = request.PriceCents;
        product.SalePercent = request.SalePercent;
        product.WarrantyDays = request.WarrantyDays;

        await _context.SaveChangesAsync(cancellationToken);

        await _homePageCache.InvalidateHomeAsync(cancellationToken);

        product.Category = await _context.Categories.FindAsync([product.CategoryId], cancellationToken);
        return Result.Succeed(ToResponse(product));
    }

    public async Task<Result> SetStockAsync(int id, StockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity < 0)
            return Result.Failure(Error.BadRequest("Product.InvalidQuantity", "Quantity cannot be negative"));

        var product = await _context.Products.FindAsync([id], cancellationToken);
        if (product is null || product.DeletedAt is not null)
            return Result.Failure(CatalogErrors.Product.NotFound);

        product.Quantity = request.Quantity;
        await _context.SaveChangesAsync(cancellationToken);

        await _homePageCache.InvalidateHomeAsync(cancellationToken);

        return Result.Succeed();
    }

    public async Task<Result> DeleteAsync(int id, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync([id], cancellationToken);
        if (product is null || product.DeletedAt is not null)
            return Result.Failure(CatalogErrors.Product.NotFound);

        product.DeletedAt = DateTime.UtcNow;
        product.Quantity = 0;

        _context.DeletesHistory.Add(new DeleteHistory
        {
            DeleterId = actor.GetUserId(),
            EntityType = nameof(Product),
            EntityId = product.Id
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _homePageCache.InvalidateHomeAsync(cancellationToken);

        return Result.Succeed();
    }

    private async Task<Product?> FindAsync(int id, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Media)
            .AsQueryable();

        if (!trackChanges)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    private static ProductManagementResponse ToResponse(Product p)
        => new(
            p.Id,
            p.Name,
            p.Sku,
            p.Description,
            p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.Quantity,
            p.Views,
            p.PriceCents,
            p.SalePercent,
            p.FinalPriceCents,
            p.WarrantyDays,
            p.DeletedAt is not null,
            p.CreatedAt,
            [.. p.Media.Select(m => m.Url)]);
}
