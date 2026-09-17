using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Users.Queries;
using Tripory.Application.UseCases.V1.Users.Responses;
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Users.Handlers;

public class GetCurrentUserProfileQueryHandler : IQueryHandler<GetCurrentUserProfileQuery, UserProfileResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserProfileQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetCurrentUserProfileQuery request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<UserProfileResponse>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập để xem thông tin."));

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, ct);
        if (user is null)
            return Result.Failure<UserProfileResponse>(new Error("User.NotFound", "Không tìm thấy người dùng."));

        var roles = user.UserRoles.Select(r => ((UserRoleType)r.RoleId).ToString()).ToList();

        var response = new UserProfileResponse(
            user.Id,
            user.Email.Value,
            user.Handle.Value,
            user.FullName,
            user.Bio,
            user.AvatarUrl,
            user.Status.ToString(),
            roles,
            user.CreatedAt
        );

        return Result.Success(response);
    }
}

