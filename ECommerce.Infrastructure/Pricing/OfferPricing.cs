using ECommerce.Infrastructure.Entities;

namespace ECommerce.Infrastructure.Pricing;

/// <summary>
/// Resolves the effective per-product sale discount when sellers may tie
/// time-bounded offers to products. The effective discount is the larger of
/// the product's base <c>SalePercent</c> and any currently active offer on it.
/// </summary>
public static class OfferPricing
{
    /// <summary>
    /// Returns the effective sale percent: the maximum of the product's base
    /// <paramref name="basePercent"/> and the best <paramref name="offerPercent"/>
    /// (0 when the product has no applicable offer), clamped to [0, 99].
    /// </summary>
    public static int EffectiveSalePercent(int basePercent, int offerPercent)
        => Math.Clamp(Math.Max(Math.Max(0, basePercent), offerPercent), 0, 99);

    public static long EffectiveFinalPriceCents(long priceCents, int effectiveSalePercent)
        => priceCents * (100 - effectiveSalePercent) / 100;

    /// <summary>
    /// Computes the effective sale percent and final price for a product using its
    /// base <c>SalePercent</c> and the best active offer discount for its id (if any).
    /// </summary>
    public static (int SalePercent, long FinalPriceCents) EffectivePricing(
        Product product,
        IReadOnlyDictionary<int, int> offerDiscounts)
    {
        var effectivePercent = EffectiveSalePercent(
            basePercent: product.SalePercent,
            offerPercent: offerDiscounts.GetValueOrDefault(product.Id));
        return (effectivePercent, EffectiveFinalPriceCents(product.PriceCents, effectivePercent));
    }
}