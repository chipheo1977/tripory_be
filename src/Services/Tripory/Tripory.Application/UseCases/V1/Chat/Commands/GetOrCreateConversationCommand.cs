using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record GetOrCreateConversationCommand(Guid PartnerId) : ICommand<ConversationDto>;