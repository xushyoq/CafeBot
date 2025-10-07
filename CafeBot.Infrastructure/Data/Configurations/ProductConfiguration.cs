using CafeBot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeBot.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Unit)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(p => p.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsAvailable);

        // Связи
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}