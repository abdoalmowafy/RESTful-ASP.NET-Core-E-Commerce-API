using Ordering.Customer.Contracts;

namespace Ordering.Customer.Validators;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.AddressId).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).IsInEnum();

        RuleFor(x => x.Identifier)
            .NotEmpty()
            .When(x => x.PaymentMethod is PaymentMethod.CreditCard or PaymentMethod.MobileWallet)
            .WithMessage("Payment identifier is required for online payments");
    }
}

public class CreateReturnRequestValidator : AbstractValidator<CreateReturnRequest>
{
    public CreateReturnRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.AddressId).GreaterThan(0);
    }
}
