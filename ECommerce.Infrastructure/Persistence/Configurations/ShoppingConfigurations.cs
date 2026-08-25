using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.HasMany(c => c.CartProducts)
            .WithOne(cp => cp.Cart)
            .HasForeignKey(cp => cp.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.PromoCode)
            .WithMany(pc => pc.Carts)
            .HasForeignKey(c => c.PromoCodeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CartProductConfiguration : IEntityTypeConfiguration<CartProduct>
{
    public void Configure(EntityTypeBuilder<CartProduct> builder)
    {
        builder.Property(cp => cp.RowVersion).IsRowVersion();

        builder.HasOne(cp => cp.Product)
            .WithMany()
            .HasForeignKey(cp => cp.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cp => new { cp.CartId, cp.ProductId }).IsUnique();
    }
}

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.Property(pc => pc.Code).HasMaxLength(32).IsRequired();
        builder.Property(pc => pc.RowVersion).IsRowVersion();
        builder.Property(pc => pc.Description).HasMaxLength(500);
        builder.Property(pc => pc.MaxSaleCents).HasColumnType("bigint");
        builder.HasIndex(pc => pc.Code).IsUnique();

        builder.HasMany(pc => pc.EditsHistory)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
