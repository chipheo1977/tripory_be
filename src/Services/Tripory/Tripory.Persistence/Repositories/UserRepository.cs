using Microsoft.EntityFrameworkCore;
using Tripory.Application.Abstractions.Data;
using Tripory.Domain.Entities;
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
}