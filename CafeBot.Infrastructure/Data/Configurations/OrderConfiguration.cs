using CafeBot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeBot.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.ClientName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.ClientPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.GuestCount)
            .IsRequired();

        builder.Property(o => o.BookingDate)
            .IsRequired();

        builder.Property(o => o.TimeSlot)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        // Индексы
        builder.HasIndex(o => new { o.RoomId, o.BookingDate, o.TimeSlot });
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.EmployeeId);
        builder.HasIndex(o => o.ClientPhone);
        builder.HasIndex(o => o.BookingDate);

        // Связи
        builder.HasOne(o => o.Room)
            .WithMany(r => r.Orders)
            .HasForeignKey(o => o.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Employee)
            .WithMany(e => e.Orders)
            .HasForeignKey(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}