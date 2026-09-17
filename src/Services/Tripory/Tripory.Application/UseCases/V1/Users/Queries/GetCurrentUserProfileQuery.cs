using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Users.Responses;

namespace Tripory.Application.UseCases.V1.Users.Queries;

public record GetCurrentUserProfileQuery : IQuery<UserProfileResponse>;

