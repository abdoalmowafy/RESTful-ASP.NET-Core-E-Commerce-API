using Catalog.Management.Contracts;

namespace Catalog.Management.Validators;

public class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
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

public class CategoryRequestValidator : AbstractValidator<CategoryRequest>
{
    public CategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class PromoCodeRequestValidator : AbstractValidator<PromoCodeRequest>
{
    public PromoCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Percent).InclusiveBetween(0, 99);
        RuleFor(x => x.MaxSaleCents).GreaterThanOrEqualTo(0).When(x => x.MaxSaleCents.HasValue);
    }
}

public class StoreAddressRequestValidator : AbstractValidator<StoreAddressRequest>
{
    public StoreAddressRequestValidator()
    {
        RuleFor(x => x.Apartment).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Floor).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Building).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
    }
}
