namespace Tripory.Infrastructure.Configurations;

public class JwtOptions
{
    public const string SectionName = "JwtOptions";

    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpiryMinutes  { get; init; } = 15;
    public int RefreshTokenExpiryDays  { get; init; } = 7;
}