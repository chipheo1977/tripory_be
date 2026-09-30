using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Users.Commands;

using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Users.Handlers;

public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserProfileCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateUserProfileCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure(DomainErrors.Auth.Unauthorized);

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, ct);
        if (user is null)
            return Result.Failure(DomainErrors.Auth.UserNotFound);

        // Gọi phương thức Domain Entity để tự bảo vệ Invariant
        var updateResult = user.UpdateProfile(request.FullName, request.Bio, request.AvatarUrl);
        if (updateResult.IsFailure)
            return updateResult;

        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}

