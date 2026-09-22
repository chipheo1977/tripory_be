using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages", "identity");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.SenderId)
            .IsRequired();

        builder.Property(m => m.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(ChatMessage.MaxTextLength);

        builder.Property(m => m.VoiceUrl)
            .HasMaxLength(1000);

        builder.Property(m => m.VoiceDuration);

        builder.Property(m => m.IsRead)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        // Ánh xạ Value Object CallLogData
        builder.OwnsOne(m => m.CallLogData, callLogBuilder =>
        {
           callLogBuilder.Property(c => c.Status)
               .HasColumnName("call_status")
               .HasConversion<int>();

            callLogBuilder.Property(c => c.DurationSeconds)
                .HasColumnName("call_duration_seconds");

            callLogBuilder.Property(c => c.Direction)
                .HasColumnName("call_direction")
                .HasConversion<int>();
        });

        // Composite Index tối ưu truy vấn lịch sử tin nhắn
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
    }
}
