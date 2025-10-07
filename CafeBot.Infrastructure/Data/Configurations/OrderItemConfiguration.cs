using CafeBot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeBot.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(oi => oi.Quantity)
            .IsRequired()
            .HasPrecision(10, 3);

        builder.Property(oi => oi.Unit)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(oi => oi.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(oi => oi.Subtotal)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(oi => oi.OrderId);
        builder.HasIndex(oi => oi.ProductId);

        // Связи
        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(oi => oi.AddedByEmployee)
            .WithMany()
            .HasForeignKey(oi => oi.AddedByEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}