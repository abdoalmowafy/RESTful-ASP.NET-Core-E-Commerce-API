using Microsoft.AspNetCore.Hosting;
using Seller.Profile.Contracts;

namespace Seller.Profile.Services;

public interface ISellerStoreService
{
    Task<Result<StoreResponse>> GetMineAsync(string ownerId, CancellationToken cancellationToken = default);
    Task<Result<StoreResponse>> CreateAsync(string ownerId, UpsertStoreRequest request, CancellationToken cancellationToken = default);
    Task<Result<StoreResponse>> UpdateAsync(string ownerId, UpsertStoreRequest request, CancellationToken cancellationToken = default);
}

public class SellerStoreService(AppDbContext context, UserManager<ApplicationUser> userManager) : ISellerStoreService
{
    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<StoreResponse>> GetMineAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        var store = await FindAsync(ownerId, cancellationToken);
        return store is null
            ? Result.Failure<StoreResponse>(MarketplaceErrors.Store.NotFound)
            : Result.Succeed(ToResponse(store));
    }

    public async Task<Result<StoreResponse>> CreateAsync(string ownerId, UpsertStoreRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.Stores.AnyAsync(s => s.OwnerId == ownerId && s.DeletedAt == null, cancellationToken))
            return Result.Failure<StoreResponse>(MarketplaceErrors.Store.AlreadyExists);

        var slug = GenerateSlug(request.Name);
        var nameTaken = await _context.Stores.AnyAsync(
            s => s.DeletedAt == null && (s.Name == request.Name || s.Slug == slug), cancellationToken);

        if (nameTaken)
            return Result.Failure<StoreResponse>(MarketplaceErrors.Store.NameDuplicated);

        var userExists = await _context.Users.AnyAsync(u => u.Id == ownerId, cancellationToken);
        if (!userExists)
            return Result.Failure<StoreResponse>(UserErrors.NotFound);

        var store = new Store
        {
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            Status = StoreStatus.PendingVerification
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync(cancellationToken);

        _context.SellerProfiles.Add(new SellerProfile { Id = ownerId, StoreId = store.Id });
        await _context.SaveChangesAsync(cancellationToken);

        var owner = await _userManager.FindByIdAsync(ownerId);
        if (owner is not null && !await _userManager.IsInRoleAsync(owner, "Seller"))
            await _userManager.AddToRoleAsync(owner, "Seller");

        return Result.Succeed(ToResponse(store));
    }

    public async Task<Result<StoreResponse>> UpdateAsync(string ownerId, UpsertStoreRequest request, CancellationToken cancellationToken = default)
    {
        var store = await FindAsync(ownerId, cancellationToken, trackChanges: true);
        if (store is null)
            return Result.Failure<StoreResponse>(MarketplaceErrors.Store.NotFound);

        var slug = GenerateSlug(request.Name);
        var nameTaken = await _context.Stores.AnyAsync(
            s => s.DeletedAt == null && s.Id != store.Id && (s.Name == request.Name || s.Slug == slug), cancellationToken);

        if (nameTaken)
            return Result.Failure<StoreResponse>(MarketplaceErrors.Store.NameDuplicated);

        store.Name = request.Name.Trim();
        store.Slug = slug;
        store.Description = request.Description;
        store.LogoUrl = request.LogoUrl;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Succeed(ToResponse(store));
    }

    private async Task<Store?> FindAsync(string ownerId, CancellationToken ct, bool trackChanges = false)
    {
        var query = _context.Stores.AsQueryable();
        if (!trackChanges) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.OwnerId == ownerId && s.DeletedAt == null, ct);
    }

    private static string GenerateSlug(string name)
        => System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-")
            .Trim('-');

    private static StoreResponse ToResponse(Store s)
        => new(s.Id, s.Name, s.Slug, s.Description, s.LogoUrl, s.Status, s.RejectionReason);
}
