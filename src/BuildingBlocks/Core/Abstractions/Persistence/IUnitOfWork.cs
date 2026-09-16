namespace BuildingBlocks.Core.Abstractions.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}