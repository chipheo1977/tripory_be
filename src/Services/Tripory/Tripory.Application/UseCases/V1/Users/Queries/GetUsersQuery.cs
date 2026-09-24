using BuildingBlocks.Core.CQRS;
using Tripory.Application.Common.Models;

namespace Tripory.Application.UseCases.V1.Users.Queries;

public record GetUsersQuery(string? Search = null, int Limit = 50) : IQuery<IReadOnlyList<UserDto>>;
