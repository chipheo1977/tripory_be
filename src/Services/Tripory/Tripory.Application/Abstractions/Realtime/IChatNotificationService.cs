using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.Abstractions.Realtime;

public interface IChatNotificationService
{
    // Đẩy tin nhắn mới đến client người nhận
    Task SendMessageNotificationAsync(Guid recipientId, ChatMessageDto message, CancellationToken ct = default);

    // Thông báo cho người gửi biết đối phương đã xem (Read Receipt)
    Task SendMessageReadNotificationAsync(
        Guid senderId,
        Guid conversationId,
        Guid readerId,
        CancellationToken ct = default
    );

    // Cập nhật lại danh sách hội thoại của người nhận (cập nhật tin cuối, tăng unread count)
    Task SendConversationUpdatedNotificationAsync(
        Guid recipientId,
        ConversationDto conversation,
        CancellationToken ct = default
    );
}