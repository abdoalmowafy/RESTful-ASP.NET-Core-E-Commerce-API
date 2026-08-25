using Driver.Profile.Contracts;
using Driver.Profile.Validators;

namespace Driver.Profile.Validators;

public class UpdateDriverLocationRequestValidator : AbstractValidator<UpdateDriverLocationRequest>
{
    public UpdateDriverLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90d, 90d);
        RuleFor(x => x.Longitude).InclusiveBetween(-180d, 180d);
    }
}
