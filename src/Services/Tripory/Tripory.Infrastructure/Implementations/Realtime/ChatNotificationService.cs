using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Infrastructure.Hubs;

namespace Tripory.Infrastructure.Implementations.Realtime;

public class ChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatNotificationService> _logger;

    public ChatNotificationService(
        IHubContext<ChatHub> hubContext,
        ILogger<ChatNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendMessageNotificationAsync(Guid recipientId, ChatMessageDto message, CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime tin nhắn mới đến User {RecipientId} trong hội thoại {ConversationId}", recipientId, message.ConversationId);

        // 1. Gửi trực tiếp đến User cụ thể (bất kể đang ở tab nào)
        await _hubContext.Clients.User(recipientId.ToString())
            .SendAsync("ReceiveMessage", message, cancellationToken: ct);

        // 2. Gửi đồng thời vào nhóm phòng chat của hội thoại (cập nhật màn hình chat đang mở)
        await _hubContext.Clients.Group($"conv_{message.ConversationId}")
            .SendAsync("ReceiveConversationMessage", message, cancellationToken: ct);
    }

    public async Task SendMessageReadNotificationAsync(
        Guid senderId,
        Guid conversationId,
        Guid readerId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime thông báo đã xem trong hội thoại {ConversationId} đến User {SenderId}", conversationId, senderId);

        var payload = new
        {
            ConversationId = conversationId,
            ReaderId = readerId,
            ReadAt = DateTimeOffset.UtcNow
        };

        // Gửi thông báo đến người gửi ban đầu để hiển thị tick xanh
        await _hubContext.Clients.User(senderId.ToString())
            .SendAsync("MessageRead", payload, cancellationToken: ct);

        // Gửi vào room hội thoại
        await _hubContext.Clients.Group($"conv_{conversationId}")
            .SendAsync("ConversationRead", payload, cancellationToken: ct);
    }

    public async Task SendConversationUpdatedNotificationAsync(
        Guid recipientId,
        ConversationDto conversation,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime cập nhật danh sách hội thoại đến User {RecipientId}", recipientId);

        await _hubContext.Clients.User(recipientId.ToString())
            .SendAsync("ConversationUpdated", conversation, cancellationToken: ct);
    }
}
