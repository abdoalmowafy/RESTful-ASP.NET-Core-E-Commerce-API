using ECommerce.Infrastructure.Persistence;
using ECommerce.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Persistence;

namespace ECommerce.UnitTests.Persistence;

public class PhoneNumberNormalizationTests : IDisposable
{
    private readonly IServiceProvider _sp = TestHost.Build();
    private readonly AppDbContext _db;

    public PhoneNumberNormalizationTests() => _db = _sp.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Legacy_local_numbers_are_converted_to_e164()
    {
        var id = Guid.NewGuid().ToString();
        _db.Users.Add(new ApplicationUser { Id = id, FirstName = "a", LastName = "b", Email = "n1@t.test", UserName = "n1@t.test", PhoneNumber = "01111111111" });
        _db.Users.Add(new ApplicationUser { Id = Guid.NewGuid().ToString(), FirstName = "c", LastName = "d", Email = "n2@t.test", UserName = "n2@t.test", PhoneNumber = "+20122222222" });
        await _db.SaveChangesAsync();

        // invoke the same pass the seeder runs (exposed via DbSeeder entry point below)
        await DbSeeder.NormalizeStoredPhoneNumbersAsync(_db);

        var legacy = await _db.Users.FirstAsync(u => u.Email == "n1@t.test");
        Assert.StartsWith("+20", legacy.PhoneNumber);
        var modern = await _db.Users.FirstAsync(u => u.Email == "n2@t.test");
        Assert.Equal("+20122222222", modern.PhoneNumber);
    }

    public void Dispose() => (_sp as IDisposable)?.Dispose();
}
