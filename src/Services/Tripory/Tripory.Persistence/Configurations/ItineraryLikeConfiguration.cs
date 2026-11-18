using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class ItineraryLikeConfiguration : IEntityTypeConfiguration<ItineraryLike>
{
    public void Configure(EntityTypeBuilder<ItineraryLike> builder)
    {
        builder.ToTable("itinerary_likes", "itinerary");

        builder.HasKey(x => new { x.ItineraryId, x.UserId });

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.ItineraryId).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<Itinerary>()
            .WithMany()
            .HasForeignKey(d => d.ItineraryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}