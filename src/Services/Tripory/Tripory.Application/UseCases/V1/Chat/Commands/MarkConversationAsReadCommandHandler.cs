using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;
using Tripory.Domain.Entities;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class MarkConversationAsReadCommandHandler : ICommandHandler<MarkConversationAsReadCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MarkConversationAsReadCommandHandler(
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

    public async Task<Result> Handle(MarkConversationAsReadCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure(DomainErrors.Auth.Unauthorized);

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure(DomainErrors.Chat.ConversationNotFound);

        if (!conversation.IsParticipant(currentUserId))
            return Result.Failure(DomainErrors.Chat.Forbidden);

        // Lấy tất cả tin nhắn đối phương gửi mà chưa đọc
        var unreadMessages = await _chatMessageRepository.GetUnreadMessagesAsync(conversation.Id, currentUserId, ct);

        foreach (var msg in unreadMessages)
        {
            msg.MarkRead();
        }

        conversation.MarkAsRead(currentUserId);

        await _chatMessageRepository.UpdateRangeAsync(unreadMessages, ct);
        await _conversationRepository.UpdateAsync(conversation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Bắn sự kiện Read Receipt đến đối phương an toàn
        await NotifyReadReceiptSafelyAsync(conversation, currentUserId, ct);

        return Result.Success();
    }

    private async Task NotifyReadReceiptSafelyAsync(
        Conversation conversation,
        Guid currentUserId,
        CancellationToken ct)
    {
        try
        {
            var partnerId = conversation.GetPartnerId(currentUserId);
            await _chatNotificationService.SendMessageReadNotificationAsync(partnerId, conversation.Id, currentUserId, ct);
        }
        catch
        {
            // Realtime notification thất bại (SignalR offline/disconnect)
            // Không làm fail request vì trạng thái đã xem đã được lưu thành công vào CSDL.
        }
    }
}
