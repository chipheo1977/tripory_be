using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Commands;
using Tripory.Application.UseCases.V1.Chat.Extensions;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Chat.Handlers;

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

    public async Task<Result<ChatMessageDto>> Handle(SendTextMessageCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ChatMessageDto>(DomainErrors.Auth.Unauthorized);

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure<ChatMessageDto>(DomainErrors.Chat.ConversationNotFound);

        if (!conversation.IsParticipant(currentUserId))
            return Result.Failure<ChatMessageDto>(DomainErrors.Chat.Forbidden);
        
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

        var dto = message.ToDto();

        // Phát realtime notification đến người nhận
        await NotifyRecipientSafelyAsync(conversation, currentUserId, dto, ct);

        return Result.Success(dto);
    }

    private async Task NotifyRecipientSafelyAsync(
        Conversation conversation,
        Guid senderId,
        ChatMessageDto dto,
        CancellationToken ct)
    {
        try
        {
            var recipientId = conversation.GetPartnerId(senderId);
            await _chatNotificationService.SendMessageNotificationAsync(recipientId, dto, ct);
        }
        catch
        {
            // Realtime notification thất bại (SignalR offline/disconnect)
            // Không làm fail request vì tin nhắn đã được lưu thành công vào CSDL.
        }
    }
}