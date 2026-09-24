using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Common.Models;
using Tripory.Application.UseCases.V1.Users.Queries;
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Users.Handlers;

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var users = await _userRepository.SearchUsersAsync(request.Search, request.Limit, ct);

        var dtos = users.Select(u =>
        {
            var roles = u.UserRoles
                .Select(r => ((UserRoleType)r.RoleId).ToString())
                .ToList();

            if (roles.Count == 0)
            {
                roles.Add(UserRoleType.Traveler.ToString());
            }

            return new UserDto(
                u.Id,
                u.Email.Value,
                u.Handle.Value,
                u.FullName,
                u.Bio,
                u.AvatarUrl,
                u.Status.ToString(),
                roles
            );
        }).ToList();

        return Result.Success<IReadOnlyList<UserDto>>(dtos);
    }
}
