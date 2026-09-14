using BuildingBlocks.Core.Domains.Abstractions.Shared;
using MediatR;

namespace BuildingBlocks.Core.CQRS;

public interface ICommand : IRequest<Result>
{
    
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
    
}