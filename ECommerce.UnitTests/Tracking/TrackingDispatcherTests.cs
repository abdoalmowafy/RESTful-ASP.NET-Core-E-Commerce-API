using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Hubs;
using ECommerce.Infrastructure.Services;
using ECommerce.UnitTests.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests.Tracking;

public sealed class RecordingClientProxy : IClientProxy
{
    public List<(string Method, object?[] Args)> Sent { get; } = [];

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Sent.Add((method, args));
        return Task.CompletedTask;
    }
}

public sealed class FakeHubClients : IHubClients
{
    public RecordingClientProxy Proxy { get; } = new();

    public IClientProxy All => Proxy;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;

    public IClientProxy Client(string connectionId) => Proxy;

    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
    public IClientProxy Group(string groupName) => Proxy;
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
    public IClientProxy User(string userId) => Proxy;
    public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
}

public sealed class FakeGroupManager : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendToGroupAsync(string groupName, string method, object?[] args, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendToGroupsAsync(IReadOnlyList<string> groupNames, string method, object?[] args, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveFromAllGroupsAsync(string connectionId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class FakeHubContext : IHubContext<TrackingHub>
{
    public IHubClients Clients { get; } = new FakeHubClients();
    public IGroupManager Groups { get; } = new FakeGroupManager();
}

public class TrackingDispatcherTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;
    private readonly string _userId = Guid.NewGuid().ToString();

    public TrackingDispatcherTests()
    {
        _db = _sp.GetRequiredService<AppDbContext>();
        _db.Users.Add(new ApplicationUser { Id = _userId, FirstName = "Track", LastName = "Me", Email = "track@shop.test", UserName = "track@shop.test", EmailConfirmed = true });
        _db.Orders.Add(new Order { Id = 500, UserId = _userId, Status = OrderStatus.OnTheWay, DeliveryNeeded = true, AddressId = 1 });
        _db.SaveChanges();
    }

    private TrackingNotificationDispatcher Sut(FakeHubContext hub)
        => new(
            _db,
            new FirebasePushSender(Options.Create(new FcmSettings()), NullLogger<FirebasePushSender>.Instance),
            new DeviceTokenService(_db),
            hub);

    [Fact]
    public async Task Driver_location_is_broadcast_to_the_order_group()
    {
        var hub = new FakeHubContext();

        await Sut(hub).NotifyDriverLocationAsync(500, 30.05, 31.23, DateTime.UtcNow, 8);

        var sent = Assert.Single(((FakeHubClients)hub.Clients).Proxy.Sent);
        Assert.Equal("driverLocationChanged", sent.Method);
    }

    [Fact]
    public async Task Status_changes_are_broadcast_to_the_order_group()
    {
        var hub = new FakeHubContext();

        await Sut(hub).NotifyStatusAsync(500, OrderStatus.OnTheWay, "picked up", DateTime.UtcNow);

        Assert.Contains(((FakeHubClients)hub.Clients).Proxy.Sent, s => s.Method == "orderStatusChanged");
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
