using Microsoft.EntityFrameworkCore;
using Tripory.Domain.Entities;
using Tripory.Application.Abstractions.Data;

namespace Tripory.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _context;

    public ConversationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Conversation conversation, CancellationToken ct = default)
    {
        await _context.Conversations.AddAsync(conversation, ct);
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Conversation?> GetByUsersAsync(Guid userA, Guid userB, CancellationToken ct = default)
    {
        var (u1, u2) = Conversation.NormalizeParticipants(userA, userB);

        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == u1 && c.User2Id == u2, ct);
    }

    public async Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);
    }

    public Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _context.Conversations.Update(conversation);
        return Task.CompletedTask;;
    }
}