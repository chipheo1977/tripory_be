using BuildingBlocks.Core.Abstractions.Shared;
using MediatR;

namespace BuildingBlocks.Core.CQRS;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
    
}