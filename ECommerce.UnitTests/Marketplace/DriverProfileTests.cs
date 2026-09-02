using Driver.Profile.Contracts;
using Driver.Profile.Services;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Marketplace;

public class DriverProfileTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly UserManager<ApplicationUser> _users;

    public DriverProfileTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
    }

    private async Task<string> SeedUserAsync(string email = "wannabe@shop.test")
    {
        var user = new ApplicationUser
        {
            FirstName = "Danny",
            LastName = "Driver",
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            PhoneNumber = "01000000001"
        };
        Assert.True((await _users.CreateAsync(user, "Passw0rd!")).Succeeded);
        return user.Id;
    }

    private DriverProfileService Sut() => new(_users);

    [Fact]
    public async Task Apply_creates_pending_profile_without_a_role()
    {
        var userId = await SeedUserAsync();

        var result = await Sut().ApplyAsync(userId, new ApplyDriverRequest(VehicleType.Car, "XYZ 5678", "DL-77123"));

        Assert.True(result.IsSucceed);
        Assert.Equal(RegistrationStatus.PendingVerification, result.Value.Status);

        var user = await _users.FindByIdAsync(userId);
        Assert.DoesNotContain("Driver", await _users.GetRolesAsync(user!));
        Assert.NotNull(user!.DriverProfile);
    }

    [Fact]
    public async Task Second_application_is_rejected()
    {
        var userId = await SeedUserAsync();
        var sut = Sut();

        await sut.ApplyAsync(userId, new ApplyDriverRequest(VehicleType.Van, "AAA 1111", "DL-1"));
        var again = await sut.ApplyAsync(userId, new ApplyDriverRequest(VehicleType.Van, "BBB 2222", "DL-2"));

        Assert.True(again.IsFailure);
        Assert.Equal(MarketplaceErrors.DriverProfile.AlreadyApplied.Code, again.Error.Code);
    }

    [Fact]
    public async Task Rejected_driver_can_resubmit_for_verification()
    {
        var userId = await SeedUserAsync();
        var sut = Sut();

        await sut.ApplyAsync(userId, new ApplyDriverRequest(VehicleType.Motorcycle, "MOT 0101", "DL-9"));

        var user = await _users.Users.Include(u => u.DriverProfile).FirstAsync(u => u.Id == userId);
        user.DriverProfile!.RegistrationStatus = RegistrationStatus.Rejected;
        user.DriverProfile.RejectionReason = "Blurry license";
        await _users.UpdateAsync(user);

        var resubmit = await sut.ResubmitAsync(userId, new ApplyDriverRequest(VehicleType.Van, "VAN 2020", "DL-10"));

        Assert.True(resubmit.IsSucceed);
        Assert.Equal(RegistrationStatus.PendingVerification, resubmit.Value.Status);
        Assert.Null(resubmit.Value.RejectionReason);
    }

    [Fact]
    public async Task Resubmit_blocked_while_not_rejected()
    {
        var userId = await SeedUserAsync();
        await Sut().ApplyAsync(userId, new ApplyDriverRequest(VehicleType.Car, "CAR 3030", "DL-11"));

        var result = await Sut().ResubmitAsync(userId, new ApplyDriverRequest(VehicleType.Car, "CAR 4040", "DL-12"));

        Assert.True(result.IsFailure);
        Assert.Equal(MarketplaceErrors.DriverProfile.NotEditable.Code, result.Error.Code);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
