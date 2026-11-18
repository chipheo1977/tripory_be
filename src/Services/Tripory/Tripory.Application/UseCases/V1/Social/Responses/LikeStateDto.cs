namespace Tripory.Application.UseCases.V1.Social.Responses;

public record LikeStateDto(
    int LikeCount,
    bool LikedByMe
);
