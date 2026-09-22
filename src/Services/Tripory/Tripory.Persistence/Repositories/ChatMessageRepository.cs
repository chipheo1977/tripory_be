using Microsoft.EntityFrameworkCore;
using Tripory.Application.Abstractions.Data;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly ApplicationDbContext _context;

    public ChatMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetConversationMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.CreatedAt) // Đảo lại theo thứ tự thời gian tăng dần để client hiển thị
            .ToListAsync(ct);
    }

    public async Task AddAsync(ChatMessage message, CancellationToken ct = default)
    {
        await _context.ChatMessages.AddAsync(message, ct);
    }

    public Task UpdateAsync(ChatMessage message, CancellationToken ct = default)
    {
        _context.ChatMessages.Update(message);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        _context.ChatMessages.UpdateRange(messages);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetUnreadMessagesAsync(
        Guid conversationId,
        Guid receiverId,
        CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.SenderId != receiverId && !m.IsRead)
            .ToListAsync(ct);
    }
}
