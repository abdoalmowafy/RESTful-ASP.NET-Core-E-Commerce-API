using Microsoft.AspNetCore.Hosting;
using Seller.Profile.Contracts;

namespace Seller.Profile.Validators;

public class UpsertStoreRequestValidator : AbstractValidator<UpsertStoreRequest>
{
    public UpsertStoreRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.LogoUrl).MaximumLength(500);
    }
}

public class SellerProductRequestValidator : AbstractValidator<SellerProductRequest>
{
    public SellerProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Sku).NotEmpty().Matches(RegexPatterns.Sku);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceCents).GreaterThan(0);
        RuleFor(x => x.SalePercent).InclusiveBetween(0, 99);
        RuleFor(x => x.WarrantyDays).InclusiveBetween(14, 3650);
    }
}
