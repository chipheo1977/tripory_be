using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Auth.Commands;

using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Auth.Handlers;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure(DomainErrors.Auth.Unauthorized);

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, ct);
        if (user is null)
            return Result.Failure(DomainErrors.Auth.UserNotFound);

        // Xác thực mật khẩu cũ
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(DomainErrors.Auth.InvalidCurrentPassword);

        // Băm mật khẩu mới & Cập nhật qua Domain Entity
        var newHash = _passwordHasher.HashPassword(request.NewPassword);
        user.ChangePassword(newHash);

        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}

