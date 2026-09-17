using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Users.Commands;

public record UpdateUserProfileCommand(
    string FullName,
    string? Bio,
    string? AvatarUrl
) : ICommand;

