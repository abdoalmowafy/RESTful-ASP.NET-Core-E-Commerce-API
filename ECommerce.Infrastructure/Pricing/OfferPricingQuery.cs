using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Pricing;

public static class OfferPricingQuery
{
    /// <summary>
    /// Loads the best currently-active offer discount percent per product for the
    /// supplied product ids. Offers belonging to inactive or deleted stores are
    /// ignored. Returns an empty dictionary when no offers apply.
    /// </summary>
    public static async Task<Dictionary<int, int>> LoadBestOfferDiscountByProductAsync(
        this AppDbContext context,
        IReadOnlyCollection<int> productIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
            return [];

        return await context.OfferProducts
            .AsNoTracking()
            .Where(op => productIds.Contains(op.ProductId))
            .Where(op => op.Offer!.IsActive
                         && op.Offer.StartsAt <= utcNow
                         && op.Offer.EndsAt > utcNow
                         && op.Offer.Store!.DeletedAt == null
                         && op.Offer.Store.Status == StoreStatus.Active)
            .GroupBy(op => op.ProductId)
            .Select(g => new { ProductId = g.Key, Best = g.Max(op => op.Offer!.DiscountPercent) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Best, cancellationToken);
    }
}
