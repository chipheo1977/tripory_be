namespace Tripory.Application.UseCases.V1.Auth.Responses;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
