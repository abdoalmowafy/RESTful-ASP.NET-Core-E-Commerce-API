using ECommerceAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ECommerceAPI.Data;

public class DataContext(DbContextOptions<DataContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Address> StoreAddresses { get; set; }
    public DbSet<Address> UsersAddresses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartProduct> CartProducts { get; set; }
    public DbSet<PromoCode> PromoCodes { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }
    public DbSet<Return> Returns { get; set; }
    public DbSet<EditHistory> EditHistories { get; set; }
    public DbSet<DeleteHistory> DeletesHistory { get; set; }
    public DbSet<Search> Searches { get; set; }

    public int SaveChanges(User? currentUser)
    {
        var entries = ChangeTracker.Entries().ToList();
        var editedEntries = ChangeTracker.Entries().Where(e => e.State == EntityState.Modified).ToList();

        foreach (var entry in editedEntries)
        {
            var editedObj = entry.Entity;
            var entityType = editedObj.GetType();
            var editsHistoryProperty = entityType.GetProperty("EditsHistory");

            if (editsHistoryProperty is not null)
            {
                // Get the EditsHistory collection
                var editsHistory = (ICollection<EditHistory>)editsHistoryProperty.GetValue(editedObj)!;

                foreach (var prop in entry.Properties.Where(p => !Equals(p.OriginalValue, p.CurrentValue) && IsSimpleType(p.Metadata.ClrType)).ToList())
                {
                    var edit = new EditHistory
                    {
                        Editor = currentUser,
                        EditedType = entityType.Name,
                        EditedId = entityType.GetProperty("Id")!.GetValue(editedObj)!.ToString()!,
                        EditedField = prop.Metadata.Name,
                        OldData = prop.OriginalValue?.ToString()!,
                        NewData = prop.CurrentValue?.ToString()!
                    };

                    editsHistory.Add(edit);
                    EditHistories.Add(edit);
                }
            }
        }

        return base.SaveChanges();
    }

    public async Task<int> SaveChangesAsync(User? currentUser)
    {
        var entries = ChangeTracker.Entries().ToList();
        var editedEntries = ChangeTracker.Entries().Where(e => e.State == EntityState.Modified).ToList();

        foreach (var entry in editedEntries)
        {
            var editedObj = entry.Entity;
            var entityType = editedObj.GetType();
            var editsHistoryProperty = entityType.GetProperty("EditsHistory");

            if (editsHistoryProperty is not null)
            {
                // Get the EditsHistory collection
                var editsHistory = (ICollection<EditHistory>)editsHistoryProperty.GetValue(editedObj)!;

                foreach (var prop in entry.Properties.Where(p => !Equals(p.OriginalValue, p.CurrentValue) && IsSimpleType(p.Metadata.ClrType)).ToList())
                {
                    var edit = new EditHistory
                    {
                        Editor = currentUser,
                        EditedType = entityType.Name,
                        EditedId = entityType.GetProperty("Id")!.GetValue(editedObj)!.ToString()!,
                        EditedField = prop.Metadata.Name,
                        OldData = prop.OriginalValue?.ToString()!,
                        NewData = prop.CurrentValue?.ToString()!
                    };

                    editsHistory.Add(edit);
                    EditHistories.Add(edit);
                }
            }
        }

        return await base.SaveChangesAsync();
    }

    private static bool IsSimpleType(Type type) =>  type.IsPrimitive || // For basic types like int, byte, etc.
                                                    type == typeof(string) ||
                                                    type == typeof(decimal) ||
                                                    type == typeof(DateTime) ||
                                                    type == typeof(Guid) ||
                                                    type == typeof(TimeSpan) ||
                                                    type == typeof(DateTimeOffset) ||
                                                    type.IsEnum;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        Seed(builder);

        // --- User relations ---
        // Use Gender Name not Value
        builder.Entity<User>()
        .Property(u => u.Gender)
        .HasConversion<string>();

        // One User has many Addresses
        builder.Entity<User>()
            .HasMany(u => u.Addresses)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);

        // Many Users have many products in their wishlist
        builder.Entity<User>()
            .HasMany(u => u.WishList)
            .WithMany();

        // One User has many Orders
        builder.Entity<User>()
            .HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One User has many Returns
        builder.Entity<User>()
            .HasMany(u => u.Returns)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);

        // One User has many EditsHistory
        builder.Entity<User>()
            .HasMany(u => u.EditsHistory)
            .WithOne()
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Address relations ---
        // One StoreAddress has many EditsHistory
        builder.Entity<Address>()
            .HasMany(address => address.EditsHistory)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);

        // --- Cart relations ---
        // One Cart has many CartProducts
        builder.Entity<Cart>()
            .HasMany(c => c.CartProducts)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // One PromoCode has many Carts
        builder.Entity<Cart>()
            .HasOne(c => c.PromoCode)
            .WithMany();

        // --- CartProduct relations ---
        // One Product has many CartProducts
        builder.Entity<CartProduct>()
            .HasOne(cp => cp.Product)
            .WithMany();

        // --- EditHistory relations ---
        // One Editor commits many EditsHistory
        builder.Entity<EditHistory>()
            .HasOne(eh => eh.Editor)
            .WithMany()
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Order relations ---
        // One Transporter has many Orders
        builder.Entity<Order>()
            .HasOne(o => o.Transporter)
            .WithMany()
            .HasForeignKey(o => o.TransporterId)
            .OnDelete(DeleteBehavior.Restrict);

        // One Order has many OrderProducts
        builder.Entity<Order>()
            .HasMany(o => o.OrderProducts)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        //// Use PaymentMethod Name not Value
        builder.Entity<Order>()
        .Property(o => o.PaymentMethod)
        .HasConversion<string>();

        builder.Entity<Order>()
        .Property(o => o.Status)
        .HasConversion<string>();

        // One PromoCode has many Orders
        builder.Entity<Order>()
            .HasOne(o => o.PromoCode)
            .WithMany();

        // Many Orders have One Address
        builder.Entity<Order>()
            .HasOne(o => o.Address)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // --- OrderProduct relations ---
        // One Product has Many OrderProducts
        builder.Entity<OrderProduct>()
            .HasOne(op => op.Product)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // --- Product relations ---
        // One Category has many Products
        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One Product has many Reviews
        builder.Entity<Product>()
            .HasMany(p => p.Reviews)
            .WithOne(r => r.Product)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Product has many EditsHistory
        builder.Entity<Product>()
            .HasMany(p => p.EditsHistory)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);

        // --- PromoCode relations ---
        // One PromoCode has many EditHistories
        builder.Entity<PromoCode>()
            .HasMany(pc => pc.EditsHistory)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);

        // --- Returns relations ---
        // One Transporter has many Returns
        builder.Entity<Return>()
        .Property(o => o.Status)
        .HasConversion<string>();

        builder.Entity<Return>()
            .HasOne(rpo => rpo.Transporter)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // One Address has many Returns
        builder.Entity<Return>()
            .HasOne(rpo => rpo.Address)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // One Order has many Returns
        builder.Entity<Return>()
            .HasOne(rpo => rpo.Order)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // One OrderProduct has many Returns
        builder.Entity<Return>()
            .HasOne(rpo => rpo.OrderProduct)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // --- Review relations ---
        // One Reviewer has many Reviews
        builder.Entity<Review>()
            .HasOne(rev => rev.Reviewer)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // One Review has many EditHistories
        builder.Entity<Review>()
            .HasMany(r => r.EditsHistory)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // --- Search relations ---
        // One User has many Searches
        builder.Entity<Search>()
            .HasOne(s => s.User)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // One Category has many Searches
        builder.Entity<Search>()
            .HasOne(s => s.Category)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
    }




    private static void Seed(ModelBuilder builder)
    {
        // Seed Categories
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Sports, Instruments & Accessories" },
            new Category { Id = 2, Name = "Toys, Games, Video Games & Accessories" },
            new Category { Id = 3, Name = "Arts, Crafts & Sewing" },
            new Category { Id = 4, Name = "Clothing, Shoes & Jewelry" },
            new Category { Id = 5, Name = "Beauty & Personal Care" },
            new Category { Id = 6, Name = "Books" },
            new Category { Id = 7, Name = "Electronics & Accessories" },
            new Category { Id = 8, Name = "Software" },
            new Category { Id = 9, Name = "Grocery & Gourmet Food" },
            new Category { Id = 10, Name = "Home Furniture & Accessories" },
            new Category { Id = 11, Name = "Luggage & Travel Gear" },
            new Category { Id = 12, Name = "Pet Supplies" }
        );

        // Seed Products
        builder.Entity<Product>().HasData(
            // Sports, Instruments & Accessories
            new Product
            {
                Id = 1,
                Name = "Wilson Tennis Racket",
                SKU = "SPT-0001",
                Description = "High-quality tennis racket for professionals.",
                CategoryId = 1,
                Quantity = 10001,
                PriceCents = 8999,
                SalePercent = 10,
                WarrantyDays = 730,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 2,
                Name = "Yamaha Acoustic Guitar",
                SKU = "SPT-0002",
                Description = "Top-notch acoustic guitar with a smooth finish.",
                CategoryId = 1,
                Quantity = 10002,
                PriceCents = 14999,
                SalePercent = 15,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 3,
                Name = "EA sports FC24 for PS5",
                SKU = "TOY-0001",
                Description = "Latest EA sports soccer game ps5 edition.",
                CategoryId = 2,
                Quantity = 10003,
                PriceCents = 12999,
                SalePercent = 5,
                WarrantyDays = 14,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 4,
                Name = "Adidas Soccer Ball",
                SKU = "SPT-0003",
                Description = "Official size soccer ball for all levels.",
                CategoryId = 1,
                Quantity = 10004,
                PriceCents = 2999,
                SalePercent = 0,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 5,
                Name = "Wilson Badminton Set",
                SKU = "SPT-0004",
                Description = "Complete badminton set for backyard fun.",
                CategoryId = 1,
                Quantity = 10005,
                PriceCents = 4599,
                SalePercent = 0,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },

            // Toys, Games, Video Games & Accessories
            new Product
            {
                Id = 6,
                Name = "LEGO Star Wars Set",
                SKU = "TOY-0002",
                Description = "Buildable Star Wars-themed LEGO set.",
                CategoryId = 2,
                Quantity = 20001,
                PriceCents = 7999,
                SalePercent = 5,
                WarrantyDays = 183,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 7,
                Name = "PlayStation 5 Console",
                SKU = "TOY-0003",
                Description = "Next-generation gaming console with ultra-high-speed SSD.",
                CategoryId = 2,
                Quantity = 20002,
                PriceCents = 49999,
                SalePercent = 0,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 8,
                Name = "Xbox Series X",
                SKU = "TOY-0004",
                Description = "Powerful gaming console with immersive gameplay.",
                CategoryId = 2,
                Quantity = 20003,
                PriceCents = 49999,
                SalePercent = 0,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 9,
                Name = "Nintendo Switch",
                SKU = "TOY-0005",
                Description = "Portable gaming console for versatile play.",
                CategoryId = 2,
                Quantity = 20004,
                PriceCents = 29999,
                SalePercent = 0,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 10,
                Name = "Hasbro Monopoly Game",
                SKU = "TOY-0006",
                Description = "Classic board game for family and friends.",
                CategoryId = 2,
                Quantity = 20005,
                PriceCents = 1999,
                SalePercent = 0,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },

            // Arts, Crafts & Sewing
            new Product
            {
                Id = 11,
                Name = "Singer Sewing Machine",
                SKU = "ART-0001",
                Description = "Reliable sewing machine for all skill levels.",
                CategoryId = 3,
                Quantity = 30001,
                PriceCents = 15999,
                SalePercent = 20,
                WarrantyDays = 1095,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 12,
                Name = "Cricut Maker Machine",
                SKU = "ART-0002",
                Description = "Versatile cutting machine for crafting projects.",
                CategoryId = 3,
                Quantity = 30002,
                PriceCents = 39999,
                SalePercent = 10,
                WarrantyDays = 730,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 13,
                Name = "Faber-Castell Colored Pencils",
                SKU = "ART-0003",
                Description = "High-quality colored pencils for artists.",
                CategoryId = 3,
                Quantity = 30003,
                PriceCents = 2499,
                SalePercent = 5,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 14,
                Name = "Prismacolor Markers",
                SKU = "ART-0004",
                Description = "Alcohol-based markers for smooth blending.",
                CategoryId = 3,
                Quantity = 30004,
                PriceCents = 3999,
                SalePercent = 10,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 15,
                Name = "Schmincke Watercolors",
                SKU = "ART-0005",
                Description = "Premium watercolor paints for artists.",
                CategoryId = 3,
                Quantity = 30005,
                PriceCents = 5999,
                SalePercent = 5,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },

            // Clothing, Shoes & Jewelry
            new Product
            {
                Id = 16,
                Name = "Levi's Denim Jeans",
                SKU = "CLT-0001",
                Description = "Classic straight-fit jeans for men.",
                CategoryId = 4,
                Quantity = 40001,
                PriceCents = 4999,
                SalePercent = 10,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 17,
                Name = "Nike Air Max Sneakers",
                SKU = "CLT-0002",
                Description = "Comfortable and stylish sneakers for daily wear.",
                CategoryId = 4,
                Quantity = 40002,
                PriceCents = 8999,
                SalePercent = 15,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 18,
                Name = "Calvin Klein T-shirt",
                SKU = "CLT-0003",
                Description = "Soft cotton T-shirt with modern fit.",
                CategoryId = 4,
                Quantity = 40003,
                PriceCents = 1999,
                SalePercent = 0,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 19,
                Name = "Ray-Ban Aviator Sunglasses",
                SKU = "CLT-0004",
                Description = "Iconic sunglasses with a timeless design.",
                CategoryId = 4,
                Quantity = 40004,
                PriceCents = 14999,
                SalePercent = 10,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 20,
                Name = "Michael Kors Leather Handbag",
                SKU = "CLT-0005",
                Description = "Luxury leather handbag with modern style.",
                CategoryId = 4,
                Quantity = 40005,
                PriceCents = 29999,
                SalePercent = 5,
                WarrantyDays = 730,
                CreatedDateTime = new(2024, 1, 1)
            },

            // Beauty & Personal Care
            new Product
            {
                Id = 21,
                Name = "Revlon Hair Dryer",
                SKU = "BPC-0001",
                Description = "Powerful hair dryer with multiple heat settings.",
                CategoryId = 5,
                Quantity = 50001,
                PriceCents = 3999,
                SalePercent = 10,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 22,
                Name = "Olay Regenerist Cream",
                SKU = "BPC-0002",
                Description = "Anti-aging cream for daily use.",
                CategoryId = 5,
                Quantity = 50002,
                PriceCents = 2999,
                SalePercent = 5,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 23,
                Name = "Philips Electric Shaver",
                SKU = "BPC-0003",
                Description = "Cordless electric shaver with precision blades.",
                CategoryId = 5,
                Quantity = 50003,
                PriceCents = 7999,
                SalePercent = 15,
                WarrantyDays = 730,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 24,
                Name = "Oral-B Electric Toothbrush",
                SKU = "BPC-0004",
                Description = "Rechargeable toothbrush with multiple brush heads.",
                CategoryId = 5,
                Quantity = 50004,
                PriceCents = 5999,
                SalePercent = 10,
                WarrantyDays = 730,
                CreatedDateTime = new(2024, 1, 1)
            },
            new Product
            {
                Id = 25,
                Name = "Dove Body Wash",
                SKU = "BPC-0005",
                Description = "Moisturizing body wash for soft skin.",
                CategoryId = 5,
                Quantity = 50005,
                PriceCents = 1299,
                SalePercent = 0,
                WarrantyDays = 365,
                CreatedDateTime = new(2024, 1, 1)
            });

        // Seed PromoCodes
        builder.Entity<PromoCode>().HasData(
            new PromoCode
            {
                Id = 1,
                Code = "SUMMER2024",
                Description = "SUMMER2024",
                Percent = 10,
                MaxSaleCents = 5000,
                CreatedDateTime = new(2024, 1, 1)
            },
            new PromoCode
            {
                Id = 2,
                Code = "WELCOME10",
                Description = "WELCOME10",
                Percent = 10,
                CreatedDateTime = new(2024, 1, 1)
            },
            new PromoCode
            {
                Id = 3,
                Code = "HOLIDAY25",
                Description = "HOLIDAY25",
                Percent = 25,
                MaxSaleCents = 15000,
                CreatedDateTime = new(2024, 1, 1)
            },
            new PromoCode
            {
                Id = 4,
                Code = "SPRING2024",
                Description = "SPRING2024",
                Percent = 15,
                MaxSaleCents = 8000,
                CreatedDateTime = new(2024, 1, 1)
            });
    }
}
