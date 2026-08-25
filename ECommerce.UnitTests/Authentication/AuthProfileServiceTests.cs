using ECommerce.Authentication.Contracts;
using ECommerce.Authentication.Services;
using ECommerce.Infrastructure.Entities;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Authentication;

public class AuthProfileServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly UserManager<ApplicationUser> _users;

    public AuthProfileServiceTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
    }

    private async Task<ApplicationUser> SeedUserAsync(string email = "profile@shop.test", string phone = "01111112222")
    {
        var user = new ApplicationUser
        {
            FirstName = "Old",
            LastName = "Name",
            Email = email,
            UserName = email,
            PhoneNumber = phone,
            EmailConfirmed = true
        };

        Assert.True((await _users.CreateAsync(user, "Passw0rd!")).Succeeded);
        return user;
    }

    [Fact]
    public async Task Get_returns_the_authenticated_users_profile()
    {
        var user = await SeedUserAsync();

        var result = await new AuthProfileService(_users).GetAsync(TestActor.For(user.Id));

        Assert.True(result.IsSucceed);
        Assert.Equal("profile@shop.test", result.Value.Email);
        Assert.Equal("Old", result.Value.FirstName);
    }

    [Fact]
    public async Task Update_changes_names_phone_and_optional_fields()
    {
        var user = await SeedUserAsync();

        var result = await new AuthProfileService(_users).UpdateAsync(TestActor.For(user.Id), new UpdateProfileRequest(
            "New", "Name", "01099997777", new DateOnly(2000, 5, 15), Gender.Female));

        Assert.True(result.IsSucceed);
        Assert.Equal("New", result.Value.FirstName);
        Assert.Equal("Name", result.Value.LastName);
        Assert.Equal("01099997777", result.Value.PhoneNumber);
        Assert.Equal(new DateOnly(2000, 5, 15), result.Value.DateOfBirth);
        Assert.Equal(Gender.Female, result.Value.Gender);

        var reloaded = await _users.FindByIdAsync(user.Id);
        Assert.Equal("New Name", $"{reloaded!.FirstName} {reloaded.LastName}");
    }

    [Fact]
    public async Task Update_rejects_a_phone_owned_by_another_user()
    {
        var other = await SeedUserAsync("other@shop.test", "01055556666");
        var user = await SeedUserAsync();

        var result = await new AuthProfileService(_users).UpdateAsync(
            TestActor.For(user.Id),
            new UpdateProfileRequest("Any", "One", other.PhoneNumber));

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.PhoneDuplicated.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_allows_keeping_your_own_phone()
    {
        var user = await SeedUserAsync(phone: "01111112222");

        var result = await new AuthProfileService(_users).UpdateAsync(
            TestActor.For(user.Id),
            new UpdateProfileRequest("Same", "Phone", "01111112222"));

        Assert.True(result.IsSucceed);
    }

    [Fact]
    public async Task Missing_actor_maps_to_not_found()
    {
        var service = new AuthProfileService(_users);
        var actor = TestActor.For(Guid.NewGuid().ToString());

        var result = await service.GetAsync(actor);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound.Code, result.Error.Code);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
