using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeBot.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TelegramId)
            .IsRequired();

        builder.HasIndex(e => e.TelegramId)
            .IsUnique();

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Связи
        builder.HasMany(e => e.Orders)
            .WithOne(o => o.Employee)
            .HasForeignKey(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.ReceivedPayments)
            .WithOne(p => p.ReceivedByEmployee)
            .HasForeignKey(p => p.ReceivedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}