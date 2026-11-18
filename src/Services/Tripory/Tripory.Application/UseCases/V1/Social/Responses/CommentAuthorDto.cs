namespace Tripory.Application.UseCases.V1.Social.Responses;

public record CommentAuthorDto(
    Guid Id,
    string DisplayName,
    string? AvatarUrl
);