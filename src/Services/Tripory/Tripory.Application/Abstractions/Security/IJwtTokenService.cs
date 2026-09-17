using System.Security.Claims;
using Tripory.Domain.Entities;

namespace Tripory.Application.Abstractions.Security;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}