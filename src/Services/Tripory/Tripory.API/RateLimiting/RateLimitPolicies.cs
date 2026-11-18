namespace Tripory.API.RateLimiting;

public static class RateLimitPolicies
{
    public const string AuthLogin = "auth-login";
    public const string AuthRegister = "auth-register";
    public const string AuthRefreshToken = "auth-refresh-token";
    public const string SocialLike = "social-like";
    public const string SocialCommentCreate = "social-comment-create";
    public const string SocialCommentDelete = "social-comment-delete";
}