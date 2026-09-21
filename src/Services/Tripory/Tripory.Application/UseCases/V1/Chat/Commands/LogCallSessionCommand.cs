using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record LogCallSessionCommand(
    Guid ConversationId,
    CallStatus Status,
    int DurationSeconds,
    CallDirection Direction
) : ICommand<ChatMessageDto>;
