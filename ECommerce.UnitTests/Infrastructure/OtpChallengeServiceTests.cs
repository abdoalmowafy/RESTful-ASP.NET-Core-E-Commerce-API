using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Services;

namespace ECommerce.UnitTests.Infrastructure;

public class OtpChallengeServiceTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;
    private readonly DateTime _now = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    public OtpChallengeServiceTests()
    {
        _db = _sp.GetRequiredService<AppDbContext>();
    }

    private OtpChallengeService Sut(Func<DateTime>? clock = null)
        => new(_db, clock ?? (() => _now));

    [Fact]
    public async Task Issued_code_validates_once_then_is_consumed()
    {
        var sut = Sut();
        var otp = await sut.IssueAsync(OtpPurpose.EmailVerification, "user@shop.test");

        Assert.True(await sut.ValidateAndConsumeAsync(OtpPurpose.EmailVerification, "USER@shop.test", otp.Code));
        Assert.False(await sut.ValidateAndConsumeAsync(OtpPurpose.EmailVerification, "user@shop.test", otp.Code));
    }

    [Fact]
    public async Task Wrong_code_increments_attempts_and_locks_after_five()
    {
        var sut = Sut();
        var otp = await sut.IssueAsync(OtpPurpose.PasswordReset, "user@shop.test");

        for (var i = 0; i < 5; i++)
            Assert.False(await sut.ValidateAndConsumeAsync(OtpPurpose.PasswordReset, "user@shop.test", "000000"));

        Assert.False(await sut.ValidateAndConsumeAsync(OtpPurpose.PasswordReset, "user@shop.test", otp.Code));
    }

    [Fact]
    public async Task Expired_codes_are_rejected_even_if_correct()
    {
        var now = _now;
        var sut = Sut(() => now);

        var otp = await sut.IssueAsync(OtpPurpose.EmailVerification, "expired@shop.test");

        now = now.AddMinutes(6);
        Assert.False(await sut.ValidateAndConsumeAsync(OtpPurpose.EmailVerification, "expired@shop.test", otp.Code));
    }

    [Fact]
    public async Task New_issue_invalidates_previous_unused_code()
    {
        var sut = Sut();
        var first = await sut.IssueAsync(OtpPurpose.EmailVerification, "again@shop.test");
        var second = await sut.IssueAsync(OtpPurpose.EmailVerification, "again@shop.test");

        Assert.False(await sut.ValidateAndConsumeAsync(OtpPurpose.EmailVerification, "again@shop.test", first.Code));
        Assert.True(await sut.ValidateAndConsumeAsync(OtpPurpose.EmailVerification, "again@shop.test", second.Code));
    }

    [Fact]
    public async Task Purposes_are_isolated_per_target()
    {
        var sut = Sut();
        var emailOtp = await sut.IssueAsync(OtpPurpose.EmailVerification, "multi@shop.test");
        await sut.IssueAsync(OtpPurpose.PasswordReset, "multi@shop.test");

        Assert.False(await sut.ValidateAndConsumeAsync(OtpPurpose.PasswordReset, "multi@shop.test", emailOtp.Code));
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
