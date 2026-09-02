using ECommerce.Infrastructure.Entities;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.Property(o => o.Title).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(2000);
        builder.Property(o => o.DiscountPercent);

        builder.HasOne(o => o.Store)
            .WithMany(s => s.Offers)
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OfferProductConfiguration : IEntityTypeConfiguration<OfferProduct>
{
    public void Configure(EntityTypeBuilder<OfferProduct> builder)
    {
        builder.HasKey(op => new { op.OfferId, op.ProductId });

        builder.HasOne(op => op.Offer)
            .WithMany(o => o.Products)
            .HasForeignKey(op => op.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(op => op.Product)
            .WithMany(p => p.Offers)
            .HasForeignKey(op => op.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
