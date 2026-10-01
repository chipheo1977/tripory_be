using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Persistence.Configurations;

public class WaypointConfiguration : IEntityTypeConfiguration<Waypoint>
{
    public void Configure(EntityTypeBuilder<Waypoint> builder)
    {
        builder.ToTable("waypoints", "itinerary");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.ItineraryId).IsRequired();

        builder.Property(w => w.DayNumber).IsRequired();

        builder.Property(w => w.OrderIndex).IsRequired();

        builder.Property(w => w.Name)
            .HasConversion(n => n.Value, v => WaypointName.Create(v).Value)
            .HasMaxLength(WaypointName.MaxLength)
            .IsRequired();

        builder.Property(w => w.Address).HasMaxLength(500);

        builder.Property(w => w.Notes).HasMaxLength(2000);

        builder.Property(w => w.Coordinate)
            .HasConversion(
                coord => new Point(coord.Longitude, coord.Latitude) { SRID = 4326 },
                point => Wgs84Coordinate.Create(point.X, point.Y).Value)
            .HasColumnType("geometry(Point, 4326)")
            .HasColumnName("location")
            .IsRequired();

        builder.HasIndex(w => w.Coordinate).HasMethod("gist");

        builder.HasIndex(w => new { w.ItineraryId, w.DayNumber, w.OrderIndex });
    }
}
