using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Persistence.Configurations;

public class ItineraryConfiguration : IEntityTypeConfiguration<Itinerary>
{
    public void Configure(EntityTypeBuilder<Itinerary> builder)
    {
        builder.ToTable("itineraries", "itinerary");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.UserId).IsRequired();
        builder.HasIndex(i => i.UserId);

        builder.Property(i => i.Title)
            .HasConversion(t => t.Value, v => ItineraryTitle.Create(v).Value)
            .HasMaxLength(ItineraryTitle.MaxLength)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasConversion(
                d => d != null ? d.Value : null,
                v => v != null ? ItineraryDescription.Create(v).Value : null)
            .HasMaxLength(ItineraryDescription.MaxLength);

        builder.Property(i => i.CoverImageUrl).HasMaxLength(500);

        builder.Property(i => i.StartDate).HasColumnType("date");

        builder.Property(i => i.IsPublic).HasDefaultValue(false).IsRequired();

        builder.Property(i => i.TotalDistanceKm).HasDefaultValue(0.0).IsRequired();

        // Global Query Filter tự động lọc bỏ bản ghi đã xóa mềm (Soft Delete)
        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasMany(i => i.Days)
            .WithOne()
            .HasForeignKey(d => d.ItineraryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Days).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(i => i.Waypoints)
            .WithOne()
            .HasForeignKey(w => w.ItineraryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Waypoints).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}