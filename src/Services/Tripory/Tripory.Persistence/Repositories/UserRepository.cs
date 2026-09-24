using Microsoft.EntityFrameworkCore;
using Tripory.Application.Abstractions.Data;
using Tripory.Domain.Entities;
using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;

namespace Tripory.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User?> GetByHandleAsync(Handle handle, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Handle == handle, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<bool> IsEmailUniqueAsync(Email email, CancellationToken ct = default)
    {
        return !await _context.Users.AnyAsync(u => u.Email == email, ct);
    }

    public async Task<bool> IsHandleUniqueAsync(Handle handle, CancellationToken ct = default)
    {
        return !await _context.Users.AnyAsync(u => u.Handle == handle, ct);
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        var entry = _context.Entry(user);
        if (entry.State == EntityState.Detached)
        {
            _context.Users.Update(user);
        }
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<User>> SearchUsersAsync(string? search = null, int limit = 50, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var handleTerm = term.StartsWith('@') ? term[1..] : term;
            var pattern = $"%{term}%";
            var handlePattern = $"%{handleTerm}%";

            return await _context.Users
                .FromSqlInterpolated($"""
                    SELECT * FROM identity.users 
                    WHERE "Status" <> 3 
                    AND (
                        "FullName" ILIKE {pattern} 
                        OR "Handle" ILIKE {handlePattern} 
                        OR "Email" ILIKE {pattern}
                    )
                    ORDER BY "CreatedAt" DESC
                    LIMIT {limit}
                """)
                .Include(u => u.UserRoles)
                .ToListAsync(ct);
        }

        return await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Status != UserStatus.Banned)
            .OrderByDescending(u => u.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }
}