using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Tripory.API.Common.Responses;

namespace Tripory.API.RateLimiting;

public static class RateLimitingExtensions
{
    private const int SegmentsPerWindow = 6;

    // Chính sách Auth: phân vùng theo IP.
    private static readonly Dictionary<string, Func<AuthRateLimitOptions, RateLimitRule>> RuleSelectors = new()
    {
        [RateLimitPolicies.AuthLogin] = o => o.Login,
        [RateLimitPolicies.AuthRegister] = o => o.Register,
        [RateLimitPolicies.AuthRefreshToken] = o => o.RefreshToken,
    };

    private static readonly Dictionary<string, Func<SocialRateLimitOptions, RateLimitRule>> SocialRuleSelectors = new()
    {
        [RateLimitPolicies.SocialLike] = o => o.Like,
        [RateLimitPolicies.SocialCommentCreate] = o => o.CommentCreate,
        [RateLimitPolicies.SocialCommentDelete] = o => o.CommentDelete,
    };

    private static bool IsValid(RateLimitRule rule) => rule.PermitLimit > 0 && rule.WindowSeconds > 0;

    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOptions<AuthRateLimitOptions>()
            .Bind(configuration.GetSection(AuthRateLimitOptions.SectionName))
            .Validate(o => RuleSelectors.Values.All(selectRule => IsValid(selectRule(o))),
                "AuthRateLimitOptions: PermitLimit và WindowSeconds phải lớn hơn 0.")
            .ValidateOnStart();

        services.AddOptions<SocialRateLimitOptions>()
            .Bind(configuration.GetSection(SocialRateLimitOptions.SectionName))
            .Validate(o => SocialRuleSelectors.Values.All(selectRule => IsValid(selectRule(o))),
                "SocialRateLimitOptions: PermitLimit và WindowSeconds phải lớn hơn 0.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionResponseAsync;

            foreach (var (policyName, selectRule) in RuleSelectors)
                options.AddPolicy(policyName, context => CreateIpPartition(context, selectRule));

            foreach (var (policyName, selectRule) in SocialRuleSelectors)
                options.AddPolicy(policyName, context => CreateUserPartition(context, selectRule));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateIpPartition(
        HttpContext context,
        Func<AuthRateLimitOptions, RateLimitRule> selectRule
    )
    {
        var options = context.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        var rule = selectRule(options);
        var clientIp = ClientIpResolver.Resolve(context, options.BffSharedSecret);

        return CreateSlidingWindow(clientIp, rule);
    }

    /// <summary>
    /// Khóa "user:{id}" khi đã xác thực (cần UseAuthentication chạy trước UseRateLimiter),
    /// ngược lại "ip:{ip}". Tiền tố tránh trùng giữa id và chuỗi IP.
    /// </summary>
    private static RateLimitPartition<string> CreateUserPartition(
        HttpContext context,
        Func<SocialRateLimitOptions, RateLimitRule> selectRule
    )
    {
        var socialOptions = context.RequestServices.GetRequiredService<IOptions<SocialRateLimitOptions>>().Value;
        var rule = selectRule(socialOptions);

        // Cùng claim với CurrentUserService.UserId (JwtTokenService ghi ClaimTypes.NameIdentifier).
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return CreateSlidingWindow($"user:{userId}", rule);

        var authOptions = context.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        var clientIp = ClientIpResolver.Resolve(context, authOptions.BffSharedSecret);

        return CreateSlidingWindow($"ip:{clientIp}", rule);
    }

    private static RateLimitPartition<string> CreateSlidingWindow(string partitionKey, RateLimitRule rule)
    {
        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = rule.PermitLimit,
            Window = TimeSpan.FromSeconds(rule.WindowSeconds),
            SegmentsPerWindow = SegmentsPerWindow,
            QueueLimit = 0
        });
    }

    private static async ValueTask WriteRejectionResponseAsync(OnRejectedContext context, CancellationToken ct)
    {
        var httpContext = context.HttpContext;
        var authOptions = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        var socialOptions = httpContext.RequestServices.GetRequiredService<IOptions<SocialRateLimitOptions>>().Value;

        var retryAfterSeconds = ResolveRetryAfterSeconds(context, authOptions, socialOptions);
        if (retryAfterSeconds is not null)
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);

        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimitingExtensions));

        logger.LogWarning("Từ chối request do vượt rate limit: {Method} {Path} từ IP {ClientIp}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            ClientIpResolver.Resolve(httpContext, authOptions.BffSharedSecret));

        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse.Failure("RateLimit.TooManyRequests", "Bạn thao tác quá nhanh. Vui lòng thử lại sau."),
            ct);
    }

    private static int? ResolveRetryAfterSeconds(
        OnRejectedContext context,
        AuthRateLimitOptions authOptions,
        SocialRateLimitOptions socialOptions
    )
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            return (int)Math.Ceiling(retryAfter.TotalSeconds);

        var policyName = context.HttpContext.GetEndpoint()?.Metadata
            .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

        if (policyName is null)
            return null;

        RateLimitRule? rule = null;
        if (RuleSelectors.TryGetValue(policyName, out var selectAuthRule))
            rule = selectAuthRule(authOptions);
        else if (SocialRuleSelectors.TryGetValue(policyName, out var selectSocialRule))
            rule = selectSocialRule(socialOptions);

        if (rule is null)
            return null;

        return (int)Math.Ceiling(rule.WindowSeconds / (double)SegmentsPerWindow);
    }
}
