using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class SendTextMessageCommandHandler : ICommandHandler<SendTextMessageCommand, ChatMessageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SendTextMessageCommandHandler(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        IChatNotificationService chatNotificationService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService
    )
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _chatNotificationService = chatNotificationService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    // @TODO: Cân nhắc refactor lại blocks: validation, domain logic, persistence, notification.
    // Check tương tự cho các handler khác.
    public async Task<Result<ChatMessageDto>> Handle(SendTextMessageCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ChatMessageDto>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation == null)
            return Result.Failure<ChatMessageDto>(new Error("Conversation.NotFound", "Khong tìm thấy hội thoại."));

        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
            return Result.Failure<ChatMessageDto>(new Error("Chat.Forbidden", "Bạn không thuộc cuộc trò chuyện này."));
        
        // Gọi Domain Entity để tự bảo vệ Invariant
        var messageResult = ChatMessage.CreateText(conversation.Id, currentUserId, request.Content);
        if (messageResult.IsFailure)
            return Result.Failure<ChatMessageDto>(messageResult.Error);

        var message = messageResult.Value;

        // Lưu tin nhắn và cập nhật trạng thái hội thoại
        await _chatMessageRepository.AddAsync(message, ct);
        conversation.UpdateLastMessage(message.Id, message.Type, message.Content, message.CreatedAt, currentUserId);
        await _conversationRepository.UpdateAsync(conversation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = new ChatMessageDto(
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

        // Phát realtime notification đến người nhận
        var recipientId = conversation.GetPartnerId(currentUserId);
        await _chatNotificationService.SendMessageNotificationAsync(recipientId, dto, ct);

        return Result.Success(dto);
    }
}