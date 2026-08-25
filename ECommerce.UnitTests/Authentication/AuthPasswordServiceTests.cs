using ECommerce.Authentication.Contracts;
using ECommerce.Authentication.Services;
using ECommerce.Infrastructure.Entities;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Authentication;

public class AuthPasswordServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly UserManager<ApplicationUser> _users;

    public AuthPasswordServiceTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
    }

    private async Task<ApplicationUser> SeedUserAsync()
    {
        var user = new ApplicationUser
        {
            FirstName = "Pass",
            LastName = "Changer",
            Email = "password@shop.test",
            UserName = "password@shop.test",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            PhoneNumber = "01012345678"
        };

        Assert.True((await _users.CreateAsync(user, "OldPass1!")).Succeeded);
        return user;
    }

    [Fact]
    public async Task ChangePassword_updates_the_credentials()
    {
        var user = await SeedUserAsync();
        var sut = new AuthPasswordService(_users);

        var result = await sut.ChangePasswordAsync(TestActor.For(user.Id), new ChangePasswordRequest("OldPass1!", "NewPass2!"));

        Assert.True(result.IsSucceed);
        var verified = await _users.FindByIdAsync(user.Id);
        Assert.True(await _users.CheckPasswordAsync(verified!, "NewPass2!"));
        Assert.False(await _users.CheckPasswordAsync(verified!, "OldPass1!"));
    }

    [Fact]
    public async Task ChangePassword_with_wrong_current_password_fails()
    {
        var user = await SeedUserAsync();

        var result = await new AuthPasswordService(_users)
            .ChangePasswordAsync(TestActor.For(user.Id), new ChangePasswordRequest("WrongOld1!", "NewPass2!"));

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_rejects_same_password()
    {
        var user = await SeedUserAsync();

        var result = await new AuthPasswordService(_users)
            .ChangePasswordAsync(TestActor.For(user.Id), new ChangePasswordRequest("OldPass1!", "OldPass1!"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ChangePassword_for_missing_user_fails()
    {
        var result = await new AuthPasswordService(_users)
            .ChangePasswordAsync(TestActor.For(Guid.NewGuid().ToString()), new ChangePasswordRequest("x", "NewPass2!"));

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound.Code, result.Error.Code);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
