using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.Gender).HasConversion<string>();

        builder.HasMany(u => u.Addresses)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Cart)
            .WithOne(c => c.User)
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.WishList)
            .WithMany(p => p.WishlistedBy);

        builder.HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.DeliveriesAssigned)
            .WithOne(o => o.Transporter)
            .HasForeignKey(o => o.TransporterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.ReturnsRequested)
            .WithOne(r => r.RequestedBy)
            .HasForeignKey(r => r.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ReturnsTransported)
            .WithOne(r => r.Transporter)
            .HasForeignKey(r => r.TransporterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.Reviews)
            .WithOne(r => r.Reviewer)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Searches)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");
    }
}
