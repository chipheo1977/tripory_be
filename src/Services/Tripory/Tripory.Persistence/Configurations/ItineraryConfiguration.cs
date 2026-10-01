using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class ItineraryConfiguration : IEntityTypeConfiguration<Itinerary>
{
    public void Configure(EntityTypeBuilder<Itinerary> builder)
    {
        builder.ToTable("itineraries", "itinerary");

        builder.HasKey(i => i.Id);

        builder.Property(c => c.UserId).IsRequired();

        builder.Property(c => c.Title).IsRequired().HasMaxLength(100);

        builder.Property(c => c.CoverImageUrl).HasMaxLength(500);

        builder.Property(c => c.StartDate).HasColumnType("date").IsRequired();

        builder.Property(c => c.IsPublic).IsRequired();

        builder.Property(c => c.TotalDistanceKm).HasDefaultValue(0.0).IsRequired();

        builder.HasMany( i => i.Days).WithOne().HasForeignKey(d => d.ItineraryId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Days).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(i => i.Waypoints).WithOne().HasForeignKey(w => w.ItineraryId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Waypoints).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}