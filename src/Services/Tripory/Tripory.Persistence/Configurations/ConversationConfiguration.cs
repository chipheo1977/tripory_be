using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", "identity");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.User1Id).IsRequired();

        builder.Property(c => c.User2Id).IsRequired();

        // Unique Index: Đảm bảo chỉ tồn tại duy nhất 1 cuộc hội thoại giữa 2 user
        builder.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();

        builder.Property(c => c.LastMessageContent).HasMaxLength(2000);

        builder.Property(c => c.LastMessageType).HasConversion<int>();

        builder.Property(c => c.LastMessageAt);

        builder.Property(c => c.UnreadCountUser1)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.UnreadCountUser2)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Quan hệ 1-N với ChatMessage
        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}