using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record MarkConversationAsReadCommand(Guid ConversationId) : ICommand;
