namespace Tripory.Application.UseCases.V1.Users.Responses;

public record UserProfileResponse(
    Guid Id,
    string Email,
    string Handle,
    string FullName,
    string? Bio,
    string AvatarUrl,
    string Status,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt
);

