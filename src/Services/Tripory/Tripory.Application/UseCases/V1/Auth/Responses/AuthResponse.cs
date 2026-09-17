using Tripory.Application.Common.Models;

namespace Tripory.Application.UseCases.V1.Auth.Responses;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserDto User
);
