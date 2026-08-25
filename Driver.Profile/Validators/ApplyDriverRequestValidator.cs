using Driver.Profile.Contracts;
namespace Driver.Profile.Validators;

public class ApplyDriverRequestValidator : AbstractValidator<ApplyDriverRequest>
{
    public ApplyDriverRequestValidator()
    {
        RuleFor(x => x.VehicleType).IsInEnum();
        RuleFor(x => x.PlateNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(40);
    }
}
