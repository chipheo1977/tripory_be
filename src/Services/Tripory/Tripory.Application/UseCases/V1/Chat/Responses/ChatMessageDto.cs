using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Chat.Responses;

public record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    MessageType Type,
    string? Content,
    string? VoiceUrl,
    int? VoiceDuration,
    CallLogData? CallLogData,
    bool IsRead,
    DateTimeOffset CreatedAt
);