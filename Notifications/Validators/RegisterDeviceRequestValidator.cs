using FluentValidation;

namespace Notifications.Validators;

public class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.DeviceName).MaximumLength(200);
    }
}
