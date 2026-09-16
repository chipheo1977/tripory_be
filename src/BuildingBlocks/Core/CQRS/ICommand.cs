using BuildingBlocks.Core.Abstractions.Shared;
using MediatR;

namespace BuildingBlocks.Core.CQRS;

public interface ICommand : IRequest<Result>
{
    
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
    
}