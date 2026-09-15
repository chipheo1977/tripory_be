using MediatR;

namespace BuildingBlocks.Core.CQRS;

public interface IDomainEvent : INotification
{
    Guid Id { get; init; }
}