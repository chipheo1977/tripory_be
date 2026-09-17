using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Auth.Responses;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken
) : ICommand<TokenResponse>;
// @TODO: : ICommand<TokenResponse> là gì? để làm gì?