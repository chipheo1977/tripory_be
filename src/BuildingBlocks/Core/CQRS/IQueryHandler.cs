using BuildingBlocks.Core.Abstractions.Shared;
using MediatR;

namespace BuildingBlocks.Core.CQRS;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
    
}