using System.Security.Claims;
using Tripory.Domain.Entities;

namespace Tripory.Application.Abstractions.Security;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();

    /// <summary>
    /// Thời điểm hết hạn của Refresh Token mới cấp, tính từ hiện tại theo cấu hình <c>JwtOptions:RefreshTokenExpiryDays</c>.
    /// </summary>
    DateTimeOffset GetRefreshTokenExpiresAt();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}