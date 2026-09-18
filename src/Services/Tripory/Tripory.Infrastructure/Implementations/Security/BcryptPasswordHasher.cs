using Tripory.Application.Abstractions.Security;

namespace Tripory.Infrastructure.Implementations.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
    }
}