using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Extensions;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public class GetMessagesQueryHandler : IQueryHandler<GetMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMessagesQueryHandler(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(GetMessagesQuery request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>(new Error("Conversation.NotFound", "Không tìm thấy hội thoại."));

        // Bảo vệ bảo mật: Người dùng phải là một trong 2 thành viên
        if (!conversation.IsParticipant(currentUserId))
            return Result.Failure<IReadOnlyList<ChatMessageDto>>(new Error("Chat.Forbidden", "Bạn không có quyền truy cập vào đoạn chat này."));

        var messages = await _chatMessageRepository.GetConversationMessagesAsync(
            request.ConversationId, request.Page, request.PageSize, ct);

        var dtos = messages.Select(m => m.ToDto()).ToList();

        return Result.Success<IReadOnlyList<ChatMessageDto>>(dtos);
    }
}
