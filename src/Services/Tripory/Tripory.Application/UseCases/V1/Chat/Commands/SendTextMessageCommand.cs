using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record SendTextMessageCommand(
    Guid ConversationId,
    string Content
) : ICommand<ChatMessageDto>;