using System.Security.Claims;
using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager, context);
        var (superAdminId, adminId, sellerId, driverId, customerId) =
            await SeedUsersAsync(userManager, context);
        await SeedStoresAsync(context, adminId, sellerId);
        await SeedCatalogAsync(context);
        await NormalizeStoredPhoneNumbersInternalAsync(context);

        context.ChangeTracker.Clear();
    }

    /// <summary>
    /// One-time idempotent pass: converts legacy local-format phone numbers
    /// (01111111111) to canonical E.164 (+201111111111). Rows already in
    /// E.164 or that fail to parse are left untouched.
    /// </summary>
    /// <summary>Test/ops hook wrapping the normalization pass.</summary>
    public static Task NormalizeStoredPhoneNumbersAsync(AppDbContext context)
        => NormalizeStoredPhoneNumbersInternalAsync(context);

    private static async Task NormalizeStoredPhoneNumbersInternalAsync(AppDbContext context)
    {
        var stale = await context.Users
            .Where(u => u.PhoneNumber != null && !u.PhoneNumber.StartsWith("+"))
            .ToListAsync();

        foreach (var user in stale)
        {
            var e164 = user.PhoneNumber!.ToE164();
            if (e164 is null || e164 == user.PhoneNumber)
                continue;

            user.PhoneNumber = e164;
        }

        if (stale.Count > 0)
            await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, AppDbContext context)
    {
        var superAdminPermissions = Permissions.All;

        var adminPermissions = new[]
        {
            Permissions.Products.View, Permissions.Products.Create, Permissions.Products.Update,
            Permissions.Categories.Manage, Permissions.PromoCodes.View, Permissions.PromoCodes.Create,
            Permissions.PromoCodes.Update, Permissions.StoreAddresses.Manage,
            Permissions.Orders.View, Permissions.Orders.Update,
            Permissions.Returns.View, Permissions.Returns.Manage,
            Permissions.Stores.View, Permissions.Stores.Manage,
            Permissions.Customers.View, Permissions.Customers.Manage,
            Permissions.Sellers.View, Permissions.Sellers.Manage,
            Permissions.Drivers.View, Permissions.Drivers.Manage,
            Permissions.Admins.View
        };

        var driverPermissions = new[] { Permissions.Deliveries.Handle };

        await EnsureRoleWithPermissionsAsync(roleManager, context, "SuperAdmin", superAdminPermissions, isDefault: true);
        await EnsureRoleWithPermissionsAsync(roleManager, context, "Admin", adminPermissions, isDefault: true);
        await EnsureRoleWithPermissionsAsync(roleManager, context, "Driver", driverPermissions, isDefault: false);
        await EnsureRoleWithPermissionsAsync(roleManager, context, "Customer", [], isDefault: true);
        await EnsureRoleWithPermissionsAsync(roleManager, context, "Seller", [], isDefault: false);

        context.ChangeTracker.Clear();
    }

    private static async Task EnsureRoleWithPermissionsAsync(
        RoleManager<ApplicationRole> roleManager,
        AppDbContext context,
        string roleName,
        string[] desiredPermissions,
        bool isDefault)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new ApplicationRole { Name = roleName, IsDefault = isDefault };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create role '{roleName}'");
        }
        else if (role.IsDefault != isDefault)
        {
            role.IsDefault = isDefault;
            await roleManager.UpdateAsync(role);
        }

        var existingClaims = await context.Set<IdentityRoleClaim<string>>()
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == "permission")
            .Select(rc => rc.ClaimValue!)
            .ToListAsync();

        if (desiredPermissions.ToHashSet().SetEquals(existingClaims))
            return;

        await context.Set<IdentityRoleClaim<string>>()
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == "permission")
            .ExecuteDeleteAsync();

        foreach (var permission in desiredPermissions)
            await roleManager.AddClaimAsync(role, new Claim("permission", permission));
    }

    private static async Task<(string SuperAdminId, string AdminId, string SellerId, string DriverId, string CustomerId)>
        SeedUsersAsync(UserManager<ApplicationUser> userManager, AppDbContext context)
    {
        var superAdminId = (await CreateUserAsync(userManager, context, "Sam Super", DefaultUsers.SuperAdminEmail, DefaultUsers.SuperAdminPassword,
            "SuperAdmin", "01111111111")).id;
        var adminId = (await CreateUserAsync(userManager, context, "Adam Admin", DefaultUsers.AdminEmail, DefaultUsers.AdminPassword,
            "Admin", "01222222222", adminProfile: ("Marketplace Operations", "Operations"))).id;
        var sellerId = (await CreateUserAsync(userManager, context, "Laila Seller", DefaultUsers.SellerEmail, DefaultUsers.SellerPassword,
            "Seller", "01033334444")).id;
        var driverId = (await CreateUserAsync(userManager, context, "Tarek Transporter", DefaultUsers.DriverEmail, DefaultUsers.DriverPassword,
            "Driver", "01555556666",
            driverProfile: (VehicleType.Van, "ABC 1234", "DL-99887"))).id;
        var customerId = (await CreateUserAsync(userManager, context, "Careem Customer", DefaultUsers.CustomerEmail, DefaultUsers.CustomerPassword,
            "Customer", "01044445555")).id;

        return (superAdminId, adminId, sellerId, driverId, customerId);
    }

    private static async Task<(string id, bool created)> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext context,
        string fullName,
        string email,
        string password,
        string role,
        string phone,
        (string JobTitle, string Department)? adminProfile = null,
        (VehicleType Vehicle, string Plate, string License)? driverProfile = null)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return (existing.Id, false);

        var names = fullName.Split(' ', 2);
        var user = new ApplicationUser
        {
            FirstName = names[0],
            LastName = names.Length > 1 ? names[1] : string.Empty,
            Email = email,
            UserName = email,
            PhoneNumber = phone,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);

        switch (role)
        {
            case "Customer":
                user.CustomerProfile = new CustomerProfile { Id = user.Id };
                break;
            case "Admin":
            case "SuperAdmin":
                user.AdminProfile = new AdminProfile
                {
                    Id = user.Id,
                    JobTitle = adminProfile?.JobTitle ?? "Platform Administrator",
                    Department = adminProfile?.Department ?? "Core"
                };
                break;
case "Driver":
                    user.DriverProfile = new DriverProfile
                    {
                        Id = user.Id,
                        RegistrationStatus = RegistrationStatus.Active,
                        IsActive = true,
                        VehicleType = driverProfile?.Vehicle ?? VehicleType.Motorcycle,
                        PlateNumber = driverProfile?.Plate ?? "N/A",
                        LicenseNumber = driverProfile?.License ?? "N/A"
                    };
                    break;
        }

        if (user.Cart is null && role == "Customer")
            user.Cart = new Cart { UserId = user.Id };

        await userManager.UpdateAsync(user);
        await context.SaveChangesAsync();

        return (user.Id, true);
    }

    private static async Task SeedStoresAsync(AppDbContext context, string adminId, string sellerId)
    {
        if (!await context.Stores.AnyAsync())
        {
            context.Stores.AddRange(
                new Store
                {
                    OwnerId = adminId,
                    Name = "StoreFront Official",
                    Slug = "storefront-official",
                    Description = "First-party marketplace inventory fulfilled directly by StoreFront.",
                    Status = StoreStatus.Active
                },
                new Store
                {
                    OwnerId = sellerId,
                    Name = "TechNova",
                    Slug = "technova",
                    Description = "Independent electronics boutique inside the marketplace.",
                    Status = StoreStatus.Active
                });

            await context.SaveChangesAsync();
        }

        var officialStoreId = await context.Stores.Where(s => s.Slug == "storefront-official").Select(s => s.Id).FirstAsync();
        var techNovaStoreId = await context.Stores.Where(s => s.Slug == "technova").Select(s => s.Id).FirstAsync();

        if (!await context.SellerProfiles.AnyAsync())
        {
            context.SellerProfiles.Add(new SellerProfile { Id = sellerId, StoreId = techNovaStoreId });
            await context.SaveChangesAsync();
        }

        if (await context.Products.AnyAsync(p => p.StoreId == 0))
        {
            var electronicsCategoryIds = await context.Categories
                .Where(c => c.Name.Contains("Electronics"))
                .Select(c => c.Id)
                .ToListAsync();

            var products = await context.Products.Where(p => p.StoreId == 0).ToListAsync();
            foreach (var product in products)
                product.StoreId = electronicsCategoryIds.Contains(product.CategoryId) ? techNovaStoreId : officialStoreId;

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedCatalogAsync(AppDbContext context)
    {
        if (!await context.Addresses.AnyAsync(a => a.UserId == null))
        {
            context.Addresses.Add(new Address
            {
                Apartment = "Ground Floor",
                Floor = "0",
                Building = "Main Warehouse",
                Street = "12 Nile Corniche",
                City = "Cairo",
                State = "Cairo",
                Country = "Egypt",
                PostalCode = "11513"
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Sports, Instruments & Accessories" },
                new Category { Name = "Toys, Games, Video Games & Accessories" },
                new Category { Name = "Arts, Crafts & Sewing" },
                new Category { Name = "Clothing, Shoes & Jewelry" },
                new Category { Name = "Beauty & Personal Care" },
                new Category { Name = "Books" },
                new Category { Name = "Electronics & Accessories" },
                new Category { Name = "Software" },
                new Category { Name = "Grocery & Gourmet Food" },
                new Category { Name = "Home Furniture & Accessories" },
                new Category { Name = "Luggage & Travel Gear" },
                new Category { Name = "Pet Supplies" });
            await context.SaveChangesAsync();
        }

        if (!await context.Products.AnyAsync())
        {
            var categoryByName = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
            var officialStoreId = await context.Stores.Where(s => s.Slug == "storefront-official").Select(s => s.Id).FirstAsync();
            var techNovaStoreId = await context.Stores.Where(s => s.Slug == "technova").Select(s => s.Id).FirstAsync();
            DateTime seededAt = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            (string Category, string Name, string Sku, string Description, int Quantity, long PriceCents, int SalePercent, int WarrantyDays)[] products =
            [
                ("Sports, Instruments & Accessories", "Wilson Tennis Racket", "SPT-0001", "High-quality tennis racket for professionals.", 10001, 8999, 10, 730),
                ("Sports, Instruments & Accessories", "Yamaha Acoustic Guitar", "SPT-0002", "Top-notch acoustic guitar with a smooth finish.", 10002, 14999, 15, 365),
                ("Sports, Instruments & Accessories", "Adidas Soccer Ball", "SPT-0003", "Official size soccer ball for all levels.", 10004, 2999, 0, 365),
                ("Sports, Instruments & Accessories", "Wilson Badminton Set", "SPT-0004", "Complete badminton set for backyard fun.", 10005, 4599, 0, 365),
                ("Toys, Games, Video Games & Accessories", "EA Sports FC24 for PS5", "TOY-0001", "Latest EA Sports soccer game PS5 edition.", 10003, 12999, 5, 14),
                ("Toys, Games, Video Games & Accessories", "LEGO Star Wars Set", "TOY-0002", "Buildable Star Wars-themed LEGO set.", 20001, 7999, 5, 183),
                ("Toys, Games, Video Games & Accessories", "PlayStation 5 Console", "TOY-0003", "Next-generation gaming console with ultra-high-speed SSD.", 20002, 49999, 0, 365),
                ("Toys, Games, Video Games & Accessories", "Xbox Series X", "TOY-0004", "Powerful gaming console with immersive gameplay.", 20003, 49999, 0, 365),
                ("Toys, Games, Video Games & Accessories", "Nintendo Switch", "TOY-0005", "Portable gaming console for versatile play.", 20004, 29999, 0, 365),
                ("Toys, Games, Video Games & Accessories", "Hasbro Monopoly Game", "TOY-0006", "Classic board game for family and friends.", 20005, 1999, 0, 365),
                ("Arts, Crafts & Sewing", "Singer Sewing Machine", "ART-0001", "Reliable sewing machine for all skill levels.", 30001, 15999, 20, 1095),
                ("Arts, Crafts & Sewing", "Cricut Maker Machine", "ART-0002", "Versatile cutting machine for crafting projects.", 30002, 39999, 10, 730),
                ("Arts, Crafts & Sewing", "Faber-Castell Colored Pencils", "ART-0003", "High-quality colored pencils for artists.", 30003, 2499, 5, 365),
                ("Arts, Crafts & Sewing", "Prismacolor Markers", "ART-0004", "Alcohol-based markers for smooth blending.", 30004, 3999, 10, 365),
                ("Arts, Crafts & Sewing", "Schmincke Watercolors", "ART-0005", "Premium watercolor paints for artists.", 30005, 5999, 5, 365),
                ("Clothing, Shoes & Jewelry", "Levi's Denim Jeans", "CLT-0001", "Classic straight-fit jeans for men.", 40001, 4999, 10, 365),
                ("Clothing, Shoes & Jewelry", "Nike Air Max Sneakers", "CLT-0002", "Comfortable and stylish sneakers for daily wear.", 40002, 8999, 15, 365),
                ("Clothing, Shoes & Jewelry", "Calvin Klein T-shirt", "CLT-0003", "Soft cotton T-shirt with modern fit.", 40003, 1999, 0, 365),
                ("Clothing, Shoes & Jewelry", "Ray-Ban Aviator Sunglasses", "CLT-0004", "Iconic sunglasses with a timeless design.", 40004, 14999, 10, 365),
                ("Clothing, Shoes & Jewelry", "Michael Kors Leather Handbag", "CLT-0005", "Luxury leather handbag with modern style.", 40005, 29999, 5, 730),
                ("Beauty & Personal Care", "Revlon Hair Dryer", "BPC-0001", "Powerful hair dryer with multiple heat settings.", 50001, 3999, 10, 365),
                ("Beauty & Personal Care", "Olay Regenerist Cream", "BPC-0002", "Anti-aging cream for daily use.", 50002, 2999, 5, 365),
                ("Beauty & Personal Care", "Philips Electric Shaver", "BPC-0003", "Cordless electric shaver with precision blades.", 50003, 7999, 15, 730),
                ("Beauty & Personal Care", "Oral-B Electric Toothbrush", "BPC-0004", "Rechargeable toothbrush with multiple brush heads.", 50004, 5999, 10, 730),
                ("Beauty & Personal Care", "Dove Body Wash", "BPC-0005", "Moisturizing body wash for soft skin.", 50005, 1299, 0, 365),
                ("Electronics & Accessories", "Anker Power Bank", "ELC-0001", "20000mAh fast-charging power bank.", 60001, 3499, 10, 365),
                ("Electronics & Accessories", "Logitech MX Master 3S", "ELC-0002", "Wireless performance mouse with silent clicks.", 60002, 5499, 0, 730),
                ("Electronics & Accessories", "Samsung 27-inch Monitor", "ELC-0003", "QHD monitor with 165Hz refresh rate.", 60003, 18999, 5, 730),
                ("Books", "Clean Code", "BOK-0001", "A handbook of agile software craftsmanship.", 70001, 2599, 0, 14),
                ("Books", "The Pragmatic Programmer", "BOK-0002", "Your journey to mastery, 20th anniversary edition.", 70002, 2799, 10, 14)
            ];

            foreach (var p in products)
            {
                context.Products.Add(new Product
                {
                    Name = p.Name,
                    Sku = p.Sku,
                    Description = p.Description,
                    CategoryId = categoryByName[p.Category],
                    StoreId = p.Category.Contains("Electronics") ? techNovaStoreId : officialStoreId,
                    Quantity = p.Quantity,
                    PriceCents = p.PriceCents,
                    SalePercent = p.SalePercent,
                    WarrantyDays = p.WarrantyDays,
                    CreatedAt = seededAt
                });
            }

            await context.SaveChangesAsync();
        }

        if (!await context.PromoCodes.AnyAsync())
        {
            context.PromoCodes.AddRange(
                new PromoCode { Code = "SUMMER2024", Description = "Summer sale - 10% off up to 50 EGP", Percent = 10, MaxSaleCents = 5000, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PromoCode { Code = "WELCOME10", Description = "Welcome discount - flat 10% off", Percent = 10, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PromoCode { Code = "HOLIDAY25", Description = "Holiday special - 25% off up to 150 EGP", Percent = 25, MaxSaleCents = 15000, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PromoCode { Code = "SPRING2024", Description = "Spring sale - 15% off up to 80 EGP", Percent = 15, MaxSaleCents = 8000, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

            await context.SaveChangesAsync();
        }
    }
}
