using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class ItineraryDayConfiguration : IEntityTypeConfiguration<ItineraryDay>
{
    public void Configure(EntityTypeBuilder<ItineraryDay> builder)
    {
        builder.ToTable("itinerary_days", "itinerary");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.ItineraryId).IsRequired();

        builder.Property(d => d.DayNumber).IsRequired();

        builder.Property(d => d.Subtitle).HasMaxLength(100);

        builder.Property(d => d.DayDistanceKm).HasDefaultValue(0.0).IsRequired();

        builder.HasIndex(d => new { d.ItineraryId, d.DayNumber }).IsUnique();
    }
}
