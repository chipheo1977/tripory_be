using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Auth.Responses;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<AuthResponse>;
