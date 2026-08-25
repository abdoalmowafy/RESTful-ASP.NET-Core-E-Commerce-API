using Customer.Management.Contracts;
using Customer.Management.Services;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Marketplace;

public class CustomerManagementTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly UserManager<ApplicationUser> _users;

    public CustomerManagementTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
    }

    private async Task<(ApplicationUser User, ICustomerManagementService Sut)> SeedAsync()
    {
        var user = new ApplicationUser
        {
            FirstName = "Careem",
            LastName = "Customer",
            Email = "cm@shop.test",
            UserName = "cm@shop.test",
            EmailConfirmed = true,
            CustomerProfile = new CustomerProfile { Id = Guid.NewGuid().ToString() }
        };
        // profile must share the user's id (shared-PK pattern)
        user.CustomerProfile.Id = user.Id;

        Assert.True((await _users.CreateAsync(user, "Passw0rd!")).Succeeded);
        await _users.UpdateAsync(user);

        return (user, new CustomerManagementService(_users));
    }

    [Fact]
    public async Task Suspending_customer_disables_the_account()
    {
        var (user, sut) = await SeedAsync();

        var result = await sut.UpdateStatusAsync(user.Id, new UpdateCustomerStatusRequest(ProfileStatus.Suspended));

        Assert.True(result.IsSucceed);
        var reloaded = await _users.Users.Include(u => u.CustomerProfile).FirstAsync(u => u.Id == user.Id);
        Assert.Equal(ProfileStatus.Suspended, reloaded.CustomerProfile!.Status);
        Assert.True(reloaded.IsDisabled);
    }

    [Fact]
    public async Task Reactivating_restores_access()
    {
        var (user, sut) = await SeedAsync();
        user.CustomerProfile!.Status = ProfileStatus.Suspended;
        user.IsDisabled = true;
        await _users.UpdateAsync(user);

        var result = await sut.UpdateStatusAsync(user.Id, new UpdateCustomerStatusRequest(ProfileStatus.Active));

        Assert.True(result.IsSucceed);
        var reloaded = await _users.Users.Include(u => u.CustomerProfile).FirstAsync(u => u.Id == user.Id);
        Assert.Equal(ProfileStatus.Active, reloaded.CustomerProfile!.Status);
        Assert.False(reloaded.IsDisabled);
    }

    [Fact]
    public async Task Unknown_customer_id_fails()
    {
        var (_, sut) = await SeedAsync();

        var result = await sut.UpdateStatusAsync(Guid.NewGuid().ToString(), new UpdateCustomerStatusRequest(ProfileStatus.Suspended));

        Assert.True(result.IsFailure);
        Assert.Equal(MarketplaceErrors.Profiles.CustomerNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Listing_returns_only_customers()
    {
        var (user, sut) = await SeedAsync();

        var staff = new ApplicationUser
        {
            FirstName = "Staff",
            LastName = "Member",
            Email = "staff@shop.test",
            UserName = "staff@shop.test",
            EmailConfirmed = true
        };
        Assert.True((await _users.CreateAsync(staff, "Passw0rd!")).Succeeded);

        var page = await sut.GetAsync(null, 1, 10);

        Assert.Contains(page.Value.Items, c => c.Id == user.Id);
        Assert.DoesNotContain(page.Value.Items, c => c.Id == staff.Id);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
