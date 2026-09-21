using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class LogCallSessionCommandHandler : ICommandHandler<LogCallSessionCommand, ChatMessageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LogCallSessionCommandHandler(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        IChatNotificationService chatNotificationService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _chatNotificationService = chatNotificationService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ChatMessageDto>> Handle(LogCallSessionCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ChatMessageDto>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure<ChatMessageDto>(new Error("Conversation.NotFound", "Không tìm thấy hội thoại."));

        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
            return Result.Failure<ChatMessageDto>(new Error("Chat.Forbidden", "Bạn không thuộc cuộc trò chuyện này."));

        var callLogData = new CallLogData(request.Status, request.DurationSeconds, request.Direction);
        var messageResult = ChatMessage.CreateCallLog(conversation.Id, currentUserId, callLogData);
        if (messageResult.IsFailure)
            return Result.Failure<ChatMessageDto>(messageResult.Error);

        var message = messageResult.Value;

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

        var recipientId = conversation.GetPartnerId(currentUserId);
        await _chatNotificationService.SendMessageNotificationAsync(recipientId, dto, ct);

        return Result.Success(dto);
    }
}
