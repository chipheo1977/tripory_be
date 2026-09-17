namespace Tripory.Application.Common.Models;

public record UserDto(
    Guid Id,
    string Email,
    string Handle,
    string FullName,
    string? Bio,
    string AvatarUrl,
    string Status,
    IReadOnlyList<string> Roles
);