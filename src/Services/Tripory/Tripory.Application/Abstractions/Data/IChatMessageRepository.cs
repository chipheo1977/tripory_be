using Tripory.Domain.Entities;

namespace Tripory.Application.Abstractions.Data;

public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessage>> GetConversationMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
    Task AddAsync(ChatMessage message, CancellationToken ct = default);
    Task UpdateAsync(ChatMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessage>> GetUnreadMessagesAsync(Guid conversationId, Guid receiverId, CancellationToken ct = default);
}