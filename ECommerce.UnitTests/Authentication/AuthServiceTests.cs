using ECommerce.Authentication.Contracts;
using ECommerce.Authentication.Jwt;
using ECommerce.Authentication.Services;
using ECommerce.Infrastructure.Entities;
using ECommerce.UnitTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests.Authentication;

public class AuthServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly UserManager<ApplicationUser> _users;

    public AuthServiceTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
    }

    private AuthService CreateSut()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "unit-test-signing-key-0123456789abcdef-0123456789abcdef",
            Issuer = "tests",
            Audience = "tests-client",
            ExpiryMinutes = 60
        });

        return new AuthService(_users, new JwtProvider(jwtOptions, _users, _sp.GetRequiredService<AppDbContext>()));
    }

    private async Task<ApplicationUser> SeedUserAsync(
        string email = "login@shop.test",
        string password = "Passw0rd!",
        bool disabled = false,
        bool confirmed = true)
    {
        var user = new ApplicationUser
        {
            FirstName = "Login",
            LastName = "Tester",
            Email = email,
            UserName = email,
            PhoneNumber = "01000000000",
            EmailConfirmed = confirmed,
            PhoneNumberConfirmed = confirmed,
            IsDisabled = disabled
        };

        var created = await _users.CreateAsync(user, password);
        Assert.True(created.Succeeded);
        return user;
    }

    [Fact]
    public async Task Login_with_unknown_email_fails_with_invalid_credentials()
    {
        var result = await CreateSut().LoginAsync(new LoginRequest("ghost@shop.test", "whatever"));

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task Login_with_wrong_password_fails_with_invalid_credentials()
    {
        await SeedUserAsync();

        var result = await CreateSut().LoginAsync(new LoginRequest("login@shop.test", "WrongPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task Login_with_disabled_account_is_rejected()
    {
        await SeedUserAsync(disabled: true);

        var result = await CreateSut().LoginAsync(new LoginRequest("login@shop.test", "Passw0rd!"));

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.Disabled.Code, result.Error.Code);
    }

    [Fact]
    public async Task Login_without_confirmed_contact_is_rejected()
    {
        await SeedUserAsync(confirmed: false);

        var result = await CreateSut().LoginAsync(new LoginRequest("login@shop.test", "Passw0rd!"));

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.ContactNotConfirmed.Code, result.Error.Code);
    }

    [Fact]
    public async Task Valid_login_returns_token_and_roles()
    {
        var user = await SeedUserAsync();
        await _users.AddToRoleAsync(user, "Customer");

        var result = await CreateSut().LoginAsync(new LoginRequest("login@shop.test", "Passw0rd!"));

        Assert.True(result.IsSucceed);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
        Assert.Equal(3600, result.Value.ExpiresIn);
        Assert.Contains("Customer", result.Value.Roles);
        Assert.Equal("login@shop.test", result.Value.Email);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
