using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Slug).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.LogoUrl).HasMaxLength(500);
        builder.Property(s => s.Status).HasConversion<string>();
        builder.Property(s => s.RejectionReason).HasMaxLength(500);
        builder.HasIndex(s => s.Slug).IsUnique();

        builder.HasOne(s => s.Owner)
            .WithOne(u => u.Store)
            .HasForeignKey<Store>(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Products)
            .WithOne(p => p.Store)
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
    {
        public void Configure(EntityTypeBuilder<CustomerProfile> builder)
        {
            builder.HasKey(p => p.Id);

            builder.HasOne(p => p.User)
                .WithOne(u => u.CustomerProfile)
                .HasForeignKey<CustomerProfile>(p => p.Id)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(p => p.RegistrationStatus).HasConversion<string>();
            builder.Property(p => p.IsActive);
        }
    }

public class AdminProfileConfiguration : IEntityTypeConfiguration<AdminProfile>
{
    public void Configure(EntityTypeBuilder<AdminProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.JobTitle).HasMaxLength(100);
        builder.Property(p => p.Department).HasMaxLength(100);

        builder.HasOne(p => p.User)
            .WithOne(u => u.AdminProfile)
            .HasForeignKey<AdminProfile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.User)
            .WithOne(u => u.SellerProfile)
            .HasForeignKey<SellerProfile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DriverProfileConfiguration : IEntityTypeConfiguration<DriverProfile>
{
    public void Configure(EntityTypeBuilder<DriverProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.VehicleType).HasConversion<string>();
        builder.Property(p => p.PlateNumber).HasMaxLength(20).IsRequired();
        builder.Property(p => p.LicenseNumber).HasMaxLength(40).IsRequired();
        builder.Property(p => p.RegistrationStatus).HasConversion<string>();
        builder.Property(p => p.IsActive);
        builder.Property(p => p.RejectionReason).HasMaxLength(500);

        builder.HasOne(p => p.User)
            .WithOne(u => u.DriverProfile)
            .HasForeignKey<DriverProfile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
