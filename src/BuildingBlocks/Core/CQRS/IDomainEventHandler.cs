using MediatR;

namespace BuildingBlocks.Core.CQRS;

public interface IDomainEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
    
}