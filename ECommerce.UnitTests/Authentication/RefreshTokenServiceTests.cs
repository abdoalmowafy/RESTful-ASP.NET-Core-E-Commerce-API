using ECommerce.Authentication.Jwt;
using ECommerce.Authentication.Services;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Authentication;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly UserManager<ApplicationUser> _users;
    private readonly DateTime _now = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    public RefreshTokenServiceTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
    }

    private async Task<ApplicationUser> SeedUser()
    {
        var user = new ApplicationUser
        {
            FirstName = "Refresh",
            LastName = "Tester",
            Email = $"rt-{Guid.NewGuid():N}@shop.test",
            UserName = $"rt-{Guid.NewGuid():N}@shop.test",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            PhoneNumber = "01000000000"
        };

        Assert.True((await _users.CreateAsync(user, "Passw0rd!")).Succeeded);
        return user;
    }

    private RefreshTokenService Sut(Func<DateTime>? clock = null)
        => new(
            _sp.GetRequiredService<AppDbContext>(),
            _users,
            Options.Create(new RefreshTokenOptions { LifetimeDays = 7, GraceSeconds = 5 }),
            clock ?? (() => _now));

    [Fact]
    public async Task Issue_then_rotate_produces_new_token_and_revokes_old()
    {
        var user = await SeedUser();
        var sut = Sut();

        var original = await sut.IssueAsync(user);
        var rotated = await sut.RotateAsync(original.Token);

        Assert.True(rotated.IsSucceed);
        Assert.NotEqual(original.Token, rotated.Value.Token);

        var stored = await _context().RefreshTokens.ToListAsync();
        var oldRow = stored.Single(t => t.TokenHash == RefreshTokenService.Hash(original.Token));
        Assert.NotNull(oldRow.RevokedAtUtc);
        Assert.NotNull(oldRow.ReplacedByTokenId);
        Assert.Equal(oldRow.FamilyId, (await _context().RefreshTokens.FirstAsync(t => t.TokenHash == RefreshTokenService.Hash(rotated.Value.Token))).FamilyId);
    }

    [Fact]
    public async Task Re_presenting_old_token_within_grace_returns_current_child_without_rotating_again()
    {
        var user = await SeedUser();
        var sut = Sut();

        var first = await sut.IssueAsync(user);
        await sut.RotateAsync(first.Token);          // rotate to child
        var countAfterRotation = await CountAsync();

        var replay = await sut.RotateAsync(first.Token); // race: same old token again

        Assert.True(replay.IsSucceed);
        Assert.Equal(countAfterRotation, await CountAsync());
    }

    [Fact]
    public async Task Old_token_after_grace_triggers_family_revocation()
    {
        var user = await SeedUser();
        var now = _now;
        var sut = Sut(() => now);

        var first = await sut.IssueAsync(user);
        await sut.RotateAsync(first.Token);
        var familySize = await CountAsync();

        now = _now.AddMinutes(1);                    // beyond 5s grace
        var reuse = await sut.RotateAsync(first.Token);
        Assert.True(reuse.IsFailure);

        Assert.Equal(0, await ActiveCountAsync());   // whole family dead, incl. current child
        Assert.True(familySize >= 2);
    }

    [Fact]
    public async Task Expired_token_within_grace_still_rotates()
    {
        var user = await SeedUser();
        var now = _now;
        var sut = Sut(() => now);

        var issued = await sut.IssueAsync(user);
        var row = await _context().RefreshTokens.FirstAsync(t => t.TokenHash == RefreshTokenService.Hash(issued.Token));
        row.ExpiresAtUtc = now.AddSeconds(-3);       // "just" expired
        await _context().SaveChangesAsync();

        var rotated = await sut.RotateAsync(issued.Token);

        Assert.True(rotated.IsSucceed);
        Assert.NotEqual(issued.Token, rotated.Value.Token);
    }

    [Fact]
    public async Task Expired_token_beyond_grace_is_rejected()
    {
        var user = await SeedUser();
        var now = _now;
        var sut = Sut(() => now);

        var issued = await sut.IssueAsync(user);
        var row = await _context().RefreshTokens.FirstAsync(t => t.TokenHash == RefreshTokenService.Hash(issued.Token));
        row.ExpiresAtUtc = now.AddMinutes(-10);
        await _context().SaveChangesAsync();

        var result = await sut.RotateAsync(issued.Token);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidRefreshToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task Disabled_user_refresh_is_blocked_and_family_revoked()
    {
        var user = await SeedUser();
        var sut = Sut();
        var issued = await sut.IssueAsync(user);

        user.IsDisabled = true;
        await _users.UpdateAsync(user);

        var result = await sut.RotateAsync(issued.Token);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.Disabled.Code, result.Error.Code);
        Assert.Equal(0, await ActiveCountAsync());
    }

    private AppDbContext _context() => _sp.GetRequiredService<AppDbContext>();
    private Task<int> CountAsync() => _context().RefreshTokens.CountAsync();
    private Task<int> ActiveCountAsync() => _context().RefreshTokens.CountAsync(t => t.RevokedAtUtc == null);

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
