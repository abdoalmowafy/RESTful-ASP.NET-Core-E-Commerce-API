using Admin.Management.Contracts;

namespace Admin.Management.Validators;

public class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequest>
{
    public CreateAdminRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().Matches(RegexPatterns.Email);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.PhoneNumber).Matches(RegexPatterns.PhoneNumber).When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
