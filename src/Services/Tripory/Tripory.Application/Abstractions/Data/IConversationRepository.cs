using Tripory.Domain.Entities;

namespace Tripory.Application.Abstractions.Data;

public interface IConversationRepository
{
    Task<Conversation?> GetBydIdAsync(Guid id, CancellationToken ct = default);
    Task<Conversation?> GetByUsersAsync(Guid userA, Guid userB, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task UpdateAsync(Conversation conversation, CancellationToken ct = default);
}