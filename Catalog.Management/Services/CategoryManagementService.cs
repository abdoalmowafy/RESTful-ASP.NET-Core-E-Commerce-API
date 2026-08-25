using Catalog.Management.Contracts;

namespace Catalog.Management.Services;

public interface ICategoryManagementService
{
    Task<Result<IReadOnlyList<CategoryManagementResponse>>> GetAsync(CancellationToken cancellationToken = default);
    Task<Result<CategoryManagementResponse>> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CategoryManagementResponse>> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}

public class CategoryManagementService(AppDbContext context) : ICategoryManagementService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<IReadOnlyList<CategoryManagementResponse>>> GetAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryManagementResponse(c.Id, c.Name, c.Products.Count))
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<CategoryManagementResponse>>(categories);
    }

    public async Task<Result<CategoryManagementResponse>> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.Categories.AnyAsync(c => c.Name == request.Name, cancellationToken))
            return Result.Failure<CategoryManagementResponse>(CatalogErrors.Category.NameDuplicated);

        var category = new Category { Name = request.Name };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(new CategoryManagementResponse(category.Id, category.Name, 0));
    }

    public async Task<Result<CategoryManagementResponse>> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
            return Result.Failure<CategoryManagementResponse>(CatalogErrors.Category.NotFound);

        if (await _context.Categories.AnyAsync(c => c.Name == request.Name && c.Id != id, cancellationToken))
            return Result.Failure<CategoryManagementResponse>(CatalogErrors.Category.NameDuplicated);

        category.Name = request.Name;
        await _context.SaveChangesAsync(cancellationToken);

        var productsCount = await _context.Products.CountAsync(p => p.CategoryId == id, cancellationToken);
        return Result.Succeed(new CategoryManagementResponse(id, category.Name, productsCount));
    }

    public async Task<Result> DeleteAsync(int id, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
            return Result.Failure(CatalogErrors.Category.NotFound);

        if (await _context.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
            return Result.Failure(CatalogErrors.Category.HasProducts);

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed();
    }
}
