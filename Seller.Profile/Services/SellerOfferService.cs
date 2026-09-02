using Seller.Profile.Contracts;

namespace Seller.Profile.Services;

public interface ISellerOfferService
{
    Task<Result<PaginatedList<SellerOfferResponse>>> GetAsync(string ownerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<SellerOfferResponse>> CreateAsync(string ownerId, UpsertOfferRequest request, CancellationToken cancellationToken = default);
    Task<Result<SellerOfferResponse>> UpdateAsync(string ownerId, int offerId, UpsertOfferRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetActiveAsync(string ownerId, int offerId, bool isActive, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string ownerId, int offerId, CancellationToken cancellationToken = default);
}

public class SellerOfferService(AppDbContext context, HomePageCache homePageCache) : ISellerOfferService
{
    private readonly AppDbContext _context = context;
    private readonly HomePageCache _homePageCache = homePageCache;

    public async Task<Result<PaginatedList<SellerOfferResponse>>> GetAsync(
        string ownerId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var storeId = await StoreIdForOwnerAsync(ownerId, cancellationToken);
        if (storeId is null)
            return Result.Failure<PaginatedList<SellerOfferResponse>>(MarketplaceErrors.Store.NotFound);

        var page = await PaginatedList<Offer>.CreateAsync(
            _context.Offers
                .AsNoTracking()
                .Where(o => o.StoreId == storeId)
                .OrderByDescending(o => o.CreatedAt),
            pageIndex, pageSize, cancellationToken);

        var offerIds = page.Items.Select(o => o.Id).ToList();
        var productIdsByOffer = await _context.OfferProducts
            .AsNoTracking()
            .Where(op => offerIds.Contains(op.OfferId))
            .GroupBy(op => op.OfferId)
            .Select(g => new { OfferId = g.Key, ProductIds = g.Select(op => op.ProductId).ToList() })
            .ToDictionaryAsync(x => x.OfferId, x => (IReadOnlyList<int>)x.ProductIds, cancellationToken);

        var mapped = page.Items.Select(o => ToResponse(o, productIdsByOffer.GetValueOrDefault(o.Id) ?? [])).ToList();

        return Result.Succeed(new PaginatedList<SellerOfferResponse>(
            mapped, page.PageNumber, page.TotalCount, pageSize));
    }

    public async Task<Result<SellerOfferResponse>> CreateAsync(
        string ownerId,
        UpsertOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        var storeResult = await RequireActiveStoreAsync(ownerId, cancellationToken);
        if (storeResult.IsFailure)
            return Result.Failure<SellerOfferResponse>(storeResult.Error);
        var store = storeResult.Value;

        if (request.EndsAt <= request.StartsAt)
            return Result.Failure<SellerOfferResponse>(MarketplaceErrors.Offer.InvalidDates);

        if (request.ProductIds.Count == 0)
            return Result.Failure<SellerOfferResponse>(MarketplaceErrors.Offer.NoProducts);

        if (!await ProductsOwnedAsync(store.Id, request.ProductIds, cancellationToken))
            return Result.Failure<SellerOfferResponse>(MarketplaceErrors.Offer.ProductNotOwned);

        var offer = new Offer
        {
            StoreId = store.Id,
            Title = request.Title.Trim(),
            Description = request.Description,
            DiscountPercent = request.DiscountPercent,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt
        };

        foreach (var productId in request.ProductIds.Distinct())
            offer.Products.Add(new OfferProduct { ProductId = productId });

        _context.Offers.Add(offer);
        await _context.SaveChangesAsync(cancellationToken);

        await _homePageCache.InvalidateHomeAsync(cancellationToken);

        return Result.Succeed(await ToResponseAsync(offer, cancellationToken));
    }

    public async Task<Result<SellerOfferResponse>> UpdateAsync(
        string ownerId,
        int offerId,
        UpsertOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        var offer = await OwnedOfferAsync(ownerId, offerId, cancellationToken, tracked: true);
        if (offer is null)
            return Result.Failure<SellerOfferResponse>(MarketplaceErrors.Offer.NotFound);

        if (request.EndsAt <= request.StartsAt)
            return Result.Failure<SellerOfferResponse>(MarketplaceErrors.Offer.InvalidDates);

        if (request.ProductIds.Count == 0)
            return Result.Failure<SellerOfferResponse>(MarketplaceErrors.Offer.NoProducts);

        if (!await ProductsOwnedAsync(offer.StoreId, request.ProductIds, cancellationToken))
            return Result.Failure<SellerOfferResponse>(MarketplaceErrors.Offer.ProductNotOwned);

        offer.Title = request.Title.Trim();
        offer.Description = request.Description;
        offer.DiscountPercent = request.DiscountPercent;
        offer.StartsAt = request.StartsAt;
        offer.EndsAt = request.EndsAt;

        var desired = request.ProductIds.Distinct().ToHashSet();

        var toRemove = offer.Products.Where(op => !desired.Contains(op.ProductId)).ToList();
        foreach (var op in toRemove)
        {
            offer.Products.Remove(op);
            _context.OfferProducts.Remove(op);
        }

        foreach (var productId in desired.Where(pid => offer.Products.All(op => op.ProductId != pid)))
        {
            var op = new OfferProduct { ProductId = productId, OfferId = offer.Id };
            offer.Products.Add(op);
            _context.OfferProducts.Add(op);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _homePageCache.InvalidateHomeAsync(cancellationToken);

        return Result.Succeed(await ToResponseAsync(offer, cancellationToken));
    }

    public async Task<Result> SetActiveAsync(
        string ownerId,
        int offerId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var offer = await OwnedOfferAsync(ownerId, offerId, cancellationToken, tracked: true);
        if (offer is null)
            return Result.Failure(MarketplaceErrors.Offer.NotFound);

        offer.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);

        await _homePageCache.InvalidateHomeAsync(cancellationToken);

        return Result.Succeed();
    }

    public async Task<Result> DeleteAsync(string ownerId, int offerId, CancellationToken cancellationToken = default)
    {
        var offer = await OwnedOfferAsync(ownerId, offerId, cancellationToken, tracked: true);
        if (offer is null)
            return Result.Failure(MarketplaceErrors.Offer.NotFound);

        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync(cancellationToken);

        await _homePageCache.InvalidateHomeAsync(cancellationToken);

        return Result.Succeed();
    }

    private async Task<Result<Store>> RequireActiveStoreAsync(string ownerId, CancellationToken ct)
    {
        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.OwnerId == ownerId && s.DeletedAt == null, ct);

        if (store is null)
            return Result.Failure<Store>(MarketplaceErrors.Store.NotFound);
        if (store.Status != StoreStatus.Active)
            return Result.Failure<Store>(MarketplaceErrors.Store.NotActive);

        return Result.Succeed<Store>(store);
    }

    private async Task<int?> StoreIdForOwnerAsync(string ownerId, CancellationToken ct)
        => await _context.Stores
            .Where(s => s.OwnerId == ownerId && s.DeletedAt == null)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<bool> ProductsOwnedAsync(int storeId, IReadOnlyList<int> productIds, CancellationToken ct)
    {
        var distinct = productIds.Distinct().ToList();
        var ownedCount = await _context.Products
            .CountAsync(p => p.StoreId == storeId && p.DeletedAt == null && distinct.Contains(p.Id), ct);

        return ownedCount == distinct.Count;
    }

    private async Task<Offer?> OwnedOfferAsync(string ownerId, int offerId, CancellationToken ct, bool tracked = false)
    {
        var query = _context.Offers.AsQueryable();
        if (!tracked) query = query.AsNoTracking();

        return await query
            .Include(o => o.Products)
            .Where(o => o.Store!.OwnerId == ownerId && o.Id == offerId)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<SellerOfferResponse> ToResponseAsync(Offer offer, CancellationToken ct)
    {
        var productIds = await _context.OfferProducts
            .Where(op => op.OfferId == offer.Id)
            .Select(op => op.ProductId)
            .ToListAsync(ct);

        return ToResponse(offer, productIds);
    }

    private static SellerOfferResponse ToResponse(Offer offer, IReadOnlyList<int> productIds)
        => new(
            offer.Id,
            offer.Title,
            offer.Description,
            offer.DiscountPercent,
            offer.StartsAt,
            offer.EndsAt,
            offer.IsActive,
            offer.CreatedAt,
            productIds);
}
