using ECommerce.Infrastructure.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, IServiceProvider serviceProvider)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductMedia> ProductMedia { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartProduct> CartProducts { get; set; }
    public DbSet<PromoCode> PromoCodes { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }
    public DbSet<ReturnRequest> ReturnRequests { get; set; }
    public DbSet<EditHistory> EditHistories { get; set; }
    public DbSet<DeleteHistory> DeletesHistory { get; set; }
    public DbSet<Search> Searches { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<Offer> Offers { get; set; }
    public DbSet<OfferProduct> OfferProducts { get; set; }
    public DbSet<CustomerProfile> CustomerProfiles { get; set; }
    public DbSet<AdminProfile> AdminProfiles { get; set; }
    public DbSet<SellerProfile> SellerProfiles { get; set; }
    public DbSet<DriverProfile> DriverProfiles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.HasDbFunction(() => PgFunctions.Unaccent(default))
            .HasName("f_unaccent");
    }

    private static readonly HashSet<string> IgnoredAuditProperties =
    [
        nameof(EditHistory.Id),
        "CreatedAt",
        "DeletedAt",
        "ReturnedAt",
        "DeliveredAt",
        "Views"
    ];

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TrackEdits();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        TrackEdits();
        return base.SaveChanges();
    }

    private void TrackEdits()
    {
        var editorId = serviceProvider.GetService<IHttpContextAccessor>()
            ?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not EntityState.Modified || entry.Entity is not IHasEditHistory audited)
                continue;

            var entityType = entry.Entity.GetType().Name;
            var entityId = entry.Property("Id").CurrentValue?.ToString() ?? string.Empty;

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey() ||
                    IgnoredAuditProperties.Contains(property.Metadata.Name) ||
                    Equals(property.OriginalValue, property.CurrentValue) ||
                    !IsSimpleType(property.Metadata.ClrType))
                    continue;

                var edit = new EditHistory
                {
                    EditorId = editorId,
                    EntityType = entityType,
                    EntityId = entityId,
                    Field = property.Metadata.Name,
                    OldValue = property.OriginalValue?.ToString() ?? string.Empty,
                    NewValue = property.CurrentValue?.ToString() ?? string.Empty
                };

                audited.EditsHistory.Add(edit);
                EditHistories.Add(edit);
            }
        }
    }

    private static bool IsSimpleType(Type type)
        => type.IsPrimitive
           || type == typeof(string)
           || type == typeof(decimal)
           || type == typeof(DateTime)
           || type == typeof(Guid)
           || type == typeof(TimeSpan)
           || type == typeof(DateTimeOffset)
           || type.IsEnum;
}
