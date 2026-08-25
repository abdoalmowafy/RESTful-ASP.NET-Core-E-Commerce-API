using ECommerce.Infrastructure.Entities;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.Property(t => t.Token).HasMaxLength(4096).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => new { t.OwnerType, t.OwnerId });
        builder.Property(t => t.OwnerType).HasConversion<string>();
        builder.Property(t => t.Platform).HasConversion<string>();
        builder.Property(t => t.DeviceName).HasMaxLength(200);

        builder.HasOne(t => t.Owner)
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
