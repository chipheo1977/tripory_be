using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.Abstractions.Data;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task<User?> GetByHandleAsync(Handle handle, CancellationToken ct = default);
    Task<bool> IsEmailUniqueAsync(Email email, CancellationToken ct = default);
    Task<bool> IsHandleUniqueAsync(Handle handle, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}