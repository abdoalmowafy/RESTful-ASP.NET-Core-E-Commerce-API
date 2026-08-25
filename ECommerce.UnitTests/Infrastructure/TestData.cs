using ECommerce.Infrastructure.Entities;
using ECommerce.UnitTests.Infrastructure;

namespace ECommerce.UnitTests.Infrastructure;

public static class TestData
{
    public static Category Category(string name = "Electronics") => new() { Name = name };

    public static Product Product(
        string name = "Test Product",
        string sku = "TST-0001",
        int quantity = 10,
        long priceCents = 100_00,
        int salePercent = 0,
        int categoryId = 1)
        => new()
        {
            Name = name,
            Sku = sku,
            Description = $"{name} description",
            CategoryId = categoryId,
            Quantity = quantity,
            PriceCents = priceCents,
            SalePercent = salePercent,
            WarrantyDays = 365
        };

    public static PromoCode PromoCode(
        string code = "TEST10",
        int percent = 10,
        long? maxSaleCents = null,
        bool active = true)
        => new()
        {
            Code = code,
            Description = $"{code} promo",
            Percent = percent,
            MaxSaleCents = maxSaleCents,
            Active = active
        };

    public static Address CustomerAddress(string userId, int id = 0)
    {
        var address = new Address
        {
            UserId = userId,
            Apartment = "Apt 1",
            Floor = "2",
            Building = "Tower A",
            Street = "Main St",
            City = "Cairo",
            State = "Cairo",
            Country = "Egypt",
            PostalCode = "12345"
        };
        return address;
    }

    public static Address StoreAddress() => new()
    {
        Apartment = "G",
        Floor = "0",
        Building = "Warehouse",
        Street = "Nile St",
        City = "Giza",
        State = "Giza",
        Country = "Egypt",
        PostalCode = "00000"
    };
}
