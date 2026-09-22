using Microsoft.Extensions.Logging;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Infrastructure.Implementations.Realtime;

public class ChatNotificationService : IChatNotificationService
{
    private readonly ILogger<ChatNotificationService> _logger;

    public ChatNotificationService(ILogger<ChatNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendMessageNotificationAsync(Guid recipientId, ChatMessageDto message, CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime tin nhắn mới đến User {RecipientId}: {Content}", recipientId, message.Content);
        return Task.CompletedTask;
    }

    public Task SendMessageReadNotificationAsync(Guid senderId, Guid conversationId, Guid readerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime thông báo đã xem trong hội thoại {ConversationId} đến User {SenderId}", conversationId, senderId);
        return Task.CompletedTask;
    }

    public Task SendConversationUpdatedNotificationAsync(Guid recipientId, ConversationDto conversation, CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime cập nhật hội thoại đến User {RecipientId}", recipientId);
        return Task.CompletedTask;
    }
}
