using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public record GetMessagesQuery(
    Guid ConversationId,
    int Page = 1,
    int PageSize = 30
): IQuery<IReadOnlyList<ChatMessageDto>>;