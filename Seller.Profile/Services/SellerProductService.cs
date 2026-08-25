using Microsoft.AspNetCore.Hosting;
using Seller.Profile.Contracts;

namespace Seller.Profile.Services;

public interface ISellerProductService
{
    Task<Result<PaginatedList<SellerProductResponse>>> GetAsync(string ownerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<SellerProductResponse>> CreateAsync(string ownerId, SellerProductRequest request, IList<IFormFile> media, CancellationToken cancellationToken = default);
    Task<Result<SellerProductResponse>> UpdateAsync(string ownerId, int productId, SellerProductRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetStockAsync(string ownerId, int productId, SellerStockRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string ownerId, int productId, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}

public class SellerProductService(AppDbContext context, IWebHostEnvironment environment) : ISellerProductService
{
    private static readonly string[] AllowedMediaExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm", ".mov"];

    private readonly AppDbContext _context = context;
    private readonly string _mediaFolder = Path.Combine(
        environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
        "media", "products");

    public async Task<Result<Store?>> GetActiveStoreAsync(string ownerId, CancellationToken cancellationToken)
    {
        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.OwnerId == ownerId && s.DeletedAt == null, cancellationToken);

        if (store is null)
            return Result.Failure<Store?>(MarketplaceErrors.Store.NotFound);

        if (store.Status != StoreStatus.Active)
            return Result.Failure<Store?>(MarketplaceErrors.Store.NotActive);

        return Result.Succeed<Store?>(store);
    }

    public async Task<Result<PaginatedList<SellerProductResponse>>> GetAsync(
        string ownerId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Store!.OwnerId == ownerId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt);

        var page = await PaginatedList<Product>.CreateAsync(query, pageIndex, pageSize, cancellationToken);
        return Result.Succeed(new PaginatedList<SellerProductResponse>(
            page.Items.Select(ToResponse).ToList(),
            page.PageNumber, page.TotalCount, page.TotalPages));
    }

    public async Task<Result<SellerProductResponse>> CreateAsync(string ownerId, SellerProductRequest request, IList<IFormFile> media, CancellationToken cancellationToken = default)
    {
        var storeResult = await GetActiveStoreAsync(ownerId, cancellationToken);
        if (storeResult.IsFailure)
            return Result.Failure<SellerProductResponse>(storeResult.Error);
        var store = storeResult.Value!;

        if (!await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
            return Result.Failure<SellerProductResponse>(CatalogErrors.Category.NotFound);

        if (await _context.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
            return Result.Failure<SellerProductResponse>(CatalogErrors.Product.SkuDuplicated);

        var product = new Product
        {
            Name = request.Name,
            Sku = request.Sku.ToUpperInvariant(),
            Description = request.Description,
            CategoryId = request.CategoryId,
            StoreId = store.Id,
            Quantity = request.Quantity,
            PriceCents = request.PriceCents,
            SalePercent = request.SalePercent,
            WarrantyDays = request.WarrantyDays
        };

        foreach (var url in await SaveMediaAsync(media))
            product.Media.Add(new ProductMedia { Url = url });

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        product.Category = await _context.Categories.FindAsync([product.CategoryId], cancellationToken);
        return Result.Succeed(ToResponse(product));
    }

    public async Task<Result<SellerProductResponse>> UpdateAsync(string ownerId, int productId, SellerProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Store)
            .Include(p => p.Category)
            .Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null || product.DeletedAt is not null || product.Store!.OwnerId != ownerId)
            return Result.Failure<SellerProductResponse>(CatalogErrors.Product.NotFound);

        if (product.Store!.Status != StoreStatus.Active)
            return Result.Failure<SellerProductResponse>(MarketplaceErrors.Store.NotActive);

        if (!await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
            return Result.Failure<SellerProductResponse>(CatalogErrors.Category.NotFound);

        if (await _context.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != productId, cancellationToken))
            return Result.Failure<SellerProductResponse>(CatalogErrors.Product.SkuDuplicated);

        product.Name = request.Name;
        product.Sku = request.Sku.ToUpperInvariant();
        product.Description = request.Description;
        product.CategoryId = request.CategoryId;
        product.Quantity = request.Quantity;
        product.PriceCents = request.PriceCents;
        product.SalePercent = request.SalePercent;
        product.WarrantyDays = request.WarrantyDays;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Succeed(ToResponse(product));
    }

    public async Task<Result> SetStockAsync(string ownerId, int productId, SellerStockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity < 0)
            return Result.Failure(Error.BadRequest("Product.InvalidQuantity", "Quantity cannot be negative"));

        var product = await _context.Products
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null, cancellationToken);

        if (product?.Store is null || product.Store.OwnerId != ownerId)
            return Result.Failure(CatalogErrors.Product.NotFound);

        if (product.Store.Status != StoreStatus.Active)
            return Result.Failure(MarketplaceErrors.Store.NotActive);

        product.Quantity = request.Quantity;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Succeed();
    }

    public async Task<Result> DeleteAsync(string ownerId, int productId, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null, cancellationToken);

        if (product?.Store is null || product.Store.OwnerId != ownerId)
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
        return Result.Succeed();
    }

    private async Task<List<string>> SaveMediaAsync(IList<IFormFile> media)
    {
        var urls = new List<string>();
        foreach (var file in media)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedMediaExtensions.Contains(extension) || file.Length == 0)
                continue;

            Directory.CreateDirectory(_mediaFolder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = File.Create(Path.Combine(_mediaFolder, fileName));
            await file.CopyToAsync(stream);
            urls.Add($"/media/products/{fileName}");
        }
        return urls;
    }

    private static SellerProductResponse ToResponse(Product p)
        => new(
            p.Id, p.Name, p.Sku, p.Category?.Name ?? string.Empty,
            p.Quantity, p.PriceCents, p.SalePercent, p.FinalPriceCents,
            p.DeletedAt is not null, p.CreatedAt);
}
