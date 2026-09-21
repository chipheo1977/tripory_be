using Tripory.Domain.Enums; 

namespace Tripory.Application.UseCases.V1.Chat.Responses;

public record ConversationDto(
    Guid Id,
    ParticipantDto Partner,
    string? LastMessageContent,
    MessageType? LastMessageType,
    DateTimeOffset? LastMessageAt,
    int UnreadCount,
    DateTimeOffset UpdatedAt
);