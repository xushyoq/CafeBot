using CafeBot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeBot.Infrastructure.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Capacity)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasIndex(r => r.Status);

        // Связи
        builder.HasMany(r => r.Orders)
            .WithOne(o => o.Room)
            .HasForeignKey(o => o.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}