namespace Tripory.API.RateLimiting;

public sealed class AuthRateLimitOptions
{
    public const string SectionName = "AuthRateLimitOptions";

    public string BffSharedSecret { get; set; } = string.Empty;
    public RateLimitRule Login { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60};
    public RateLimitRule Register { get; init; } = new() { PermitLimit = 3, WindowSeconds = 3600 };
    public RateLimitRule RefreshToken { get; init; } = new() { PermitLimit = 20, WindowSeconds = 60 };
}

public sealed class RateLimitRule
{
    public int PermitLimit { get; init; }
    public int WindowSeconds { get; init; }
}