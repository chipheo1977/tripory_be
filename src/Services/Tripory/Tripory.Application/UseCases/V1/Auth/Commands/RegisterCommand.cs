using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Auth.Responses;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName
) : ICommand<AuthResponse>;
