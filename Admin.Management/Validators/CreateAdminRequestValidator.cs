using Admin.Management.Contracts;

namespace Admin.Management.Validators;

public class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequest>
{
    public CreateAdminRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.PhoneNumber).Must(v => v.IsValidPhone()).WithMessage("A valid phone number is required").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
