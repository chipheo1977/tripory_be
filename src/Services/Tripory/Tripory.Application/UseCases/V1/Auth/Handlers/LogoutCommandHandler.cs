using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Auth.Commands;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Auth.Handlers;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure(DomainErrors.Auth.Unauthorized);

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, ct);

        // Chỉ tìm token trong danh sách của CHÍNH user đang đăng nhập (token của người khác không bao giờ được chạm tới).
        // Idempotent: token không tồn tại / đã thu hồi / đã hết hạn / của người khác đều trả về cùng một kết quả thành công,
        // để response không tiết lộ token có tồn tại hay thuộc về ai.
        if (user is null)
            return Result.Success();

        var revokeResult = user.RevokeRefreshToken(request.RefreshToken, ipAddress: null);
        if (revokeResult.IsFailure)
            return Result.Success();

        // Chỉ thu hồi đúng một token: KHÔNG gọi RevokeAllRefreshTokens và KHÔNG kích hoạt Token Reuse Detection.
        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
