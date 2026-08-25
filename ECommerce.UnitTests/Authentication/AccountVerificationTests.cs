using ECommerce.Authentication.Contracts;
using ECommerce.Authentication.Jwt;
using ECommerce.Authentication.Services;
using ECommerce.Infrastructure.Entities;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Authentication;

public class AccountVerificationTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly UserManager<ApplicationUser> _users;
    private readonly FakeDelivery _delivery = new();

    public AccountVerificationTests()
    {
        (_users, _) = TestHost.CreateIdentityAsync(_sp).GetAwaiter().GetResult();
    }

    private AccountVerificationService Sut()
    {
        var db = _sp.GetRequiredService<AppDbContext>();
        var users = _sp.GetRequiredService<UserManager<ApplicationUser>>();
        return new AccountVerificationService(
            users,
            new OtpChallengeService(db),
            _delivery,
            db,
            new RefreshTokenService(db, users, Options.Create(new RefreshTokenOptions())));
    }

    private async Task<ApplicationUser> SeedUnconfirmedAsync(string email)
    {
        var user = new ApplicationUser
        {
            FirstName = "Verify",
            LastName = "Me",
            Email = email,
            UserName = email,
            PhoneNumber = "01012345678",
            EmailConfirmed = false,
            PhoneNumberConfirmed = false
        };

        Assert.True((await _users.CreateAsync(user, "Passw0rd!")).Succeeded);
        return user;
    }

    [Fact]
    public async Task Forgot_and_reset_flow_changes_password_and_kills_sessions()
    {
        var user = await SeedUnconfirmedAsync("reset@shop.test");
        var sut = Sut();
        var db = _sp.GetRequiredService<AppDbContext>();

        // active session exists
        var rt = new RefreshToken { TokenHash = "hash-family-1", FamilyId = Guid.NewGuid(), UserId = user.Id, ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();

        await sut.ForgotPasswordAsync("reset@shop.test");
        var code = _delivery.LastEmailCode();
        Assert.False(string.IsNullOrWhiteSpace(code));

        var result = await sut.ResetPasswordAsync("reset@shop.test", code!, "NewPass2!");
        Assert.True(result.IsSucceed);

        var reloaded = await _users.FindByIdAsync(user.Id);
        Assert.True(await _users.CheckPasswordAsync(reloaded!, "NewPass2!"));
        Assert.Equal(0, await db.RefreshTokens.CountAsync(t => t.UserId == user.Id && t.RevokedAtUtc == null));
    }

    [Fact]
    public async Task Verify_email_flips_the_flag_using_the_emailed_code()
    {
        await SeedUnconfirmedAsync("verify@shop.test");
        var sut = Sut();

        await sut.SendEmailOtpAsync("verify@shop.test");
        var code = _delivery.LastEmailCode();

        var result = await sut.VerifyEmailAsync("verify@shop.test", code!);

        Assert.True(result.IsSucceed);
        Assert.True((await _users.FindByEmailAsync("verify@shop.test"))!.EmailConfirmed);
    }

    [Fact]
    public async Task Unknown_email_is_silently_accepted_without_sending()
    {
        var result = await Sut().ForgotPasswordAsync("ghost@shop.test");

        Assert.True(result.IsSucceed);
        Assert.Empty(_delivery.Emails);
    }

    [Fact]
    public async Task Wrong_verification_code_is_rejected()
    {
        await SeedUnconfirmedAsync("wrong@shop.test");
        await Sut().SendEmailOtpAsync("wrong@shop.test");

        var result = await Sut().VerifyEmailAsync("wrong@shop.test", "000000");

        Assert.True(result.IsFailure);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
