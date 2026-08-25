using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Notifications;

public class DeviceTokenServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;
    private readonly string _userId = Guid.NewGuid().ToString();

    public DeviceTokenServiceTests()
    {
        _db = _sp.GetRequiredService<AppDbContext>();
        _db.Users.Add(new ApplicationUser
        {
            Id = _userId,
            FirstName = "Push",
            LastName = "Tester",
            Email = "push@shop.test",
            UserName = "push@shop.test",
            EmailConfirmed = true
        });
        _db.SaveChanges();
    }

    private DeviceTokenService Sut() => new(_db);

    [Fact]
    public async Task Register_creates_device_row()
    {
        var result = await Sut().RegisterAsync(AppOwnerType.Customer, _userId, "fcm-token-1", DevicePlatform.Android, "Pixel 8");

        Assert.True(result.IsSucceed);
        Assert.Equal(1, await _db.DeviceTokens.CountAsync(t => t.Token == "fcm-token-1"));
    }

    [Fact]
    public async Task Re_registration_upserts_and_refreshes_timestamp()
    {
        var sut = Sut();
        var first = await sut.RegisterAsync(AppOwnerType.Customer, _userId, "fcm-dup", DevicePlatform.Ios, null);
        var row = await _db.DeviceTokens.FirstAsync(t => t.Id == first.Value);

        var before = row.LastRegisteredAtUtc;
        await Task.Delay(5);
        var second = await sut.RegisterAsync(AppOwnerType.Customer, _userId, "fcm-dup", DevicePlatform.Ios, "renamed");

        Assert.Equal(first.Value, second.Value);                       // same row
        Assert.Equal(1, await _db.DeviceTokens.CountAsync(t => t.Token == "fcm-dup"));
        var reloaded = await _db.DeviceTokens.FirstAsync(t => t.Id == first.Value);
        Assert.True(reloaded.LastRegisteredAtUtc > before);            // refreshed
        Assert.Equal("renamed", reloaded.DeviceName);
    }

    [Fact]
    public async Task Unregister_removes_only_the_matching_token()
    {
        var sut = Sut();
        await sut.RegisterAsync(AppOwnerType.Customer, _userId, "keep-me", DevicePlatform.Web, null);
        await sut.RegisterAsync(AppOwnerType.Customer, _userId, "kill-me", DevicePlatform.Android, null);

        await sut.UnregisterAsync("kill-me");

        Assert.Equal(1, await _db.DeviceTokens.CountAsync(t => t.OwnerId == _userId));
        Assert.NotNull(await _db.DeviceTokens.SingleOrDefaultAsync(t => t.Token == "keep-me"));
    }

    [Fact]
    public async Task Dead_token_cleanup_removes_reported_tokens()
    {
        var sut = Sut();
        await sut.RegisterAsync(AppOwnerType.Customer, _userId, "alive", DevicePlatform.Web, null);
        await sut.RegisterAsync(AppOwnerType.Customer, _userId, "dead-unregistered", DevicePlatform.Web, null);

        await sut.RemoveDeadTokensAsync(["dead-unregistered"]);

        Assert.Null(await _db.DeviceTokens.SingleOrDefaultAsync(t => t.Token == "dead-unregistered"));
        Assert.NotNull(await _db.DeviceTokens.SingleAsync(t => t.Token == "alive"));
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
