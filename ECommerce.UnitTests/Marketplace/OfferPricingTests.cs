using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Pricing;

namespace ECommerce.UnitTests.Marketplace;

public class OfferPricingTests
{
    private static Product Product(long priceCents = 100_00, int salePercent = 0)
        => new()
        {
            Name = "P",
            Sku = "SKU",
            PriceCents = priceCents,
            SalePercent = salePercent
        };

    [Fact]
    public void No_offer_uses_base_sale_percent()
    {
        var p = Product(priceCents: 100_00, salePercent: 10);
        var (salePercent, finalPrice) = OfferPricing.EffectivePricing(p, new Dictionary<int, int>());

        Assert.Equal(10, salePercent);
        Assert.Equal(90_00, finalPrice);
    }

    [Fact]
    public void Offer_higher_than_base_wins()
    {
        var p = Product(priceCents: 100_00, salePercent: 5);
        var (salePercent, finalPrice) = OfferPricing.EffectivePricing(p, new Dictionary<int, int> { [p.Id] = 25 });

        Assert.Equal(25, salePercent);
        Assert.Equal(75_00, finalPrice);
    }

    [Fact]
    public void Base_higher_than_offer_wins()
    {
        var p = Product(priceCents: 100_00, salePercent: 40);
        var (salePercent, finalPrice) = OfferPricing.EffectivePricing(p, new Dictionary<int, int> { [p.Id] = 20 });

        Assert.Equal(40, salePercent);
        Assert.Equal(60_00, finalPrice);
    }

    [Fact]
    public void Discount_is_clamped_to_99()
    {
        var p = Product(priceCents: 100_00, salePercent: 50);
        var (salePercent, _) = OfferPricing.EffectivePricing(p, new Dictionary<int, int> { [p.Id] = 100 });

        Assert.Equal(99, salePercent);
    }

    [Fact]
    public void Offer_for_another_product_is_ignored()
    {
        var p = Product(priceCents: 100_00, salePercent: 10);
        var (salePercent, _) = OfferPricing.EffectivePricing(p, new Dictionary<int, int> { [999_999] = 30 });

        Assert.Equal(10, salePercent);
    }
}
