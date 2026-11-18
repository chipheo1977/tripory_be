namespace Tripory.Application.UseCases.V1.Social.Responses;

public record CommentDto(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt,
    CommentAuthorDto? Author
);