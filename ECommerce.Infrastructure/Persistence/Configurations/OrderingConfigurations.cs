using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.TotalCents).HasColumnType("bigint");
        builder.Property(o => o.Currency).HasMaxLength(8);
        builder.Property(o => o.PaymentMethod).HasConversion<string>();
        builder.Property(o => o.Status).HasConversion<string>();
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasMany(o => o.OrderProducts)
            .WithOne(op => op.Order)
            .HasForeignKey(op => op.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Address)
            .WithMany()
            .HasForeignKey(o => o.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.PromoCode)
            .WithMany(pc => pc.Orders)
            .HasForeignKey(o => o.PromoCodeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderProductConfiguration : IEntityTypeConfiguration<OrderProduct>
{
    public void Configure(EntityTypeBuilder<OrderProduct> builder)
    {
        builder.Property(op => op.ProductPriceCents).HasColumnType("bigint");

        builder.HasOne(op => op.Product)
            .WithMany()
            .HasForeignKey(op => op.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderStatusEventConfiguration : IEntityTypeConfiguration<OrderStatusEvent>
{
    public void Configure(EntityTypeBuilder<OrderStatusEvent> builder)
    {
        builder.Property(e => e.Status).HasConversion<string>();
        builder.Property(e => e.Note).HasMaxLength(300);
        builder.HasIndex(e => e.OrderId);

        builder.HasOne(e => e.Order)
            .WithMany(o => o.StatusEvents)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.Property(r => r.Reason).HasMaxLength(1000);
        builder.Property(r => r.Status).HasConversion<string>();
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.OrderProduct)
            .WithMany()
            .HasForeignKey(r => r.OrderProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Address)
            .WithMany()
            .HasForeignKey(r => r.AddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
