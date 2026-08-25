using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Marketplace;

public static class StoreSeed
{
    public static async Task<Store> CreateAsync(
        AppDbContext db,
        string ownerId,
        StoreStatus status = StoreStatus.Active,
        string name = "Test Store")
    {
        var slug = name.ToLowerInvariant().Replace(' ', '-');
        if (await db.Stores.AnyAsync(s => s.Slug == slug))
        {
            return await db.Stores.FirstAsync(s => s.Slug == slug);
        }

        var store = new Store
        {
            OwnerId = ownerId,
            Name = name,
            Slug = $"{slug}-{Guid.NewGuid():N}".Substring(0, 40),
            Status = status
        };

        db.Stores.Add(store);
        await db.SaveChangesAsync();
        return store;
    }
}
