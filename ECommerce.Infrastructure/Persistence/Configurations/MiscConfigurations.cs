using ECommerce.Infrastructure.Entities;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.Property(a => a.Apartment).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Floor).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Building).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Street).HasMaxLength(200).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Latitude).HasColumnType("double precision");
        builder.Property(a => a.Longitude).HasColumnType("double precision");

        builder.HasMany(a => a.EditsHistory)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(r => r.Text).HasMaxLength(2000);

        builder.HasMany(r => r.EditsHistory)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EditHistoryConfiguration : IEntityTypeConfiguration<EditHistory>
{
    public void Configure(EntityTypeBuilder<EditHistory> builder)
    {
        builder.Property(e => e.EntityType).HasMaxLength(100);
        builder.Property(e => e.EntityId).HasMaxLength(64);
        builder.Property(e => e.Field).HasMaxLength(100);
        builder.Property(e => e.OldValue).HasMaxLength(2000);
        builder.Property(e => e.NewValue).HasMaxLength(2000);

        builder.HasOne(e => e.Editor)
            .WithMany()
            .HasForeignKey(e => e.EditorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
    }
}

public class DeleteHistoryConfiguration : IEntityTypeConfiguration<DeleteHistory>
{
    public void Configure(EntityTypeBuilder<DeleteHistory> builder)
    {
        builder.Property(d => d.EntityType).HasMaxLength(100);

        builder.HasOne(d => d.Deleter)
            .WithMany()
            .HasForeignKey(d => d.DeleterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SearchConfiguration : IEntityTypeConfiguration<Search>
{
    public void Configure(EntityTypeBuilder<Search> builder)
    {
        builder.Property(s => s.KeyWord).HasMaxLength(200);
    }
}

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.Property(o => o.Target).HasMaxLength(200).IsRequired();
        builder.Property(o => o.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(o => o.Purpose).HasConversion<string>();
        builder.HasIndex(o => new { o.Purpose, o.Target });
    }
}
