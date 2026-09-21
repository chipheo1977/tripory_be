using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public record GetConversationsQuery : IQuery<IReadOnlyList<ConversationDto>>;