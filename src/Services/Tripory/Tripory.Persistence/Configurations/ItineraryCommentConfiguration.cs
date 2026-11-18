using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class ItineraryCommentConfiguration : IEntityTypeConfiguration<ItineraryComment>
{
    public void Configure(EntityTypeBuilder<ItineraryComment> builder)
    {
        builder.ToTable("itinerary_comments", "itinerary");

        builder.HasKey(i => i.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasIndex(c => new { c.ItineraryId, c.CreatedAt, c.Id });

        builder.Property(x => x.Content).IsRequired().HasMaxLength(500);

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.ItineraryId).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<Itinerary>()
            .WithMany()
            .HasForeignKey(d => d.ItineraryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}