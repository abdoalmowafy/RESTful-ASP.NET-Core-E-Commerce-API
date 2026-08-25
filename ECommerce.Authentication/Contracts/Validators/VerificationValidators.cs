using ECommerce.Authentication.Contracts;
using ECommerce.Infrastructure.Abstractions;

namespace ECommerce.Authentication.Contracts.Validators;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().Matches(RegexPatterns.Email);
        RuleFor(x => x.Code).NotEmpty().Matches(@"^\d{6}$");
    }
}

public class VerifyPhoneRequestValidator : AbstractValidator<VerifyPhoneRequest>
{
    public VerifyPhoneRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Matches(@"^\d{6}$");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().Matches(RegexPatterns.Email);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Matches(@"^\d{6}$");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
