using ECommerce.Authentication.Contracts;
using ECommerce.Authentication.Services;
using ECommerce.Infrastructure.Entities;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Authentication;

public class AuthRegistrationServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly UserManager<ApplicationUser> _users;

    public AuthRegistrationServiceTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
    }

    private AuthRegistrationService CreateSut() => new(_users);

    [Fact]
    public async Task Register_creates_customer_with_cart()
    {
        var sut = CreateSut();

        var result = await sut.RegisterAsync(new RegisterRequest(
            "Sara", "Shopper", "sara@shop.test", "Passw0rd!", "01011112222"));

        Assert.True(result.IsSucceed);

        var user = await _users.FindByEmailAsync("sara@shop.test");
        Assert.NotNull(user);
        Assert.DoesNotContain("Customer", await _users.GetRolesAsync(user!));
        Assert.NotNull(user.CustomerProfile);
        Assert.Equal(RegistrationStatus.Active, user.CustomerProfile.RegistrationStatus);
    }

    [Fact]
    public async Task Register_with_duplicated_email_fails()
    {
        var sut = CreateSut();
        await sut.RegisterAsync(new RegisterRequest("First", "User", "dup@shop.test", "Passw0rd!"));

        var result = await sut.RegisterAsync(new RegisterRequest("Second", "User", "dup@shop.test", "Passw0rd!"));

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailDuplicated.Code, result.Error.Code);
    }

    [Fact]
    public async Task Register_with_duplicated_phone_fails()
    {
        var sut = CreateSut();
        await sut.RegisterAsync(new RegisterRequest("First", "User", "a@shop.test", "Passw0rd!", "01099998888"));

        var result = await sut.RegisterAsync(new RegisterRequest("Second", "User", "b@shop.test", "Passw0rd!", "01099998888"));

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.PhoneDuplicated.Code, result.Error.Code);
    }

    [Fact]
    public async Task Register_surfaces_identity_password_errors()
    {
        var sut = CreateSut();

        var result = await sut.RegisterAsync(new RegisterRequest("Weak", "Password", "weak@shop.test", "short"));

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error.StatusCode);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
