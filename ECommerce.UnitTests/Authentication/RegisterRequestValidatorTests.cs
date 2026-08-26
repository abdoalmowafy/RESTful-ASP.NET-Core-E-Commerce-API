using ECommerce.Authentication.Contracts;
using ECommerce.Authentication.Contracts.Validators;
using FluentValidation.TestHelper;

namespace ECommerce.UnitTests.Authentication;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    private static FluentValidation.TestHelper.TestValidationResult<RegisterRequest> _Validate(RegisterRequest r)
        => new RegisterRequestValidator().TestValidate(r);

    [Theory]
    [InlineData("01098765432", true)]      // EG local mobile
    [InlineData("+201098765432", true)]    // same EG number in international form
    [InlineData("+442071838750", true)]    // other valid region (UK landline)
    [InlineData("12345", false)]           // too short to be a real number
    [InlineData("abcdefghij", false)]      // not digits at all
    public async Task Phone_validation_uses_libphonenumber(string phone, bool shouldBeValid)
    {
        var request = new RegisterRequest("A", "B", "p@shop.test", "Passw0rd!", phone);
        var result = await _sut.TestValidateAsync(request);

        Assert.Equal(shouldBeValid, result.IsValid);
    }

    [Fact]
    public async Task Empty_phone_is_allowed_but_email_format_is_enforced()
    {
        var ok = await _sut.TestValidateAsync(new RegisterRequest("A", "B", "nop@shop.test", "Passw0rd!"));
        Assert.True(ok.IsValid);

        var badEmail = await _sut.TestValidateAsync(new RegisterRequest("A", "B", "not-an-email", "Passw0rd!"));
        Assert.False(badEmail.IsValid);
    }
}
