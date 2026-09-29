using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;

namespace Tripory.Application.UseCases.V1.Chat.Extensions;

public static class ChatMessageExtensions
{
    public static ChatMessageDto ToDto(this ChatMessage message)
    {
        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Type,
            message.Content,
            message.VoiceUrl,
            message.VoiceDuration,
            message.CallLogData,
            message.IsRead,
            message.CreatedAt
        );
    }
}
