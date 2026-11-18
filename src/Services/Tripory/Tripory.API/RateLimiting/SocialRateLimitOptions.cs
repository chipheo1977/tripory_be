namespace Tripory.API.RateLimiting;

public sealed class SocialRateLimitOptions
{
    public const string SectionName = "SocialRateLimitOptions";

    public RateLimitRule Like { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };
    public RateLimitRule CommentCreate { get; init; } = new() { PermitLimit = 5, WindowSeconds = 60 };
    public RateLimitRule CommentDelete { get; init; } = new() { PermitLimit = 20, WindowSeconds = 60 };
}
