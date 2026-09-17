using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : ICommand;

