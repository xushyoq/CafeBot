using CafeBot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeBot.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Method)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.HasIndex(p => p.OrderId)
            .IsUnique();

        builder.HasIndex(p => p.ReceivedByEmployeeId);

        // Связи
        builder.HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ReceivedByEmployee)
            .WithMany(e => e.ReceivedPayments)
            .HasForeignKey(p => p.ReceivedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}