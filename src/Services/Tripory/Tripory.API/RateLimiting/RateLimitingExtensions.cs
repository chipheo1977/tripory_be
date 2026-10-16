using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Tripory.API.Common.Responses;

namespace Tripory.API.RateLimiting;

public static class RateLimitingExtensions
{
    private const int SegmentsPerWindow = 6;

    private static readonly Dictionary<string, Func<AuthRateLimitOptions, RateLimitRule>> RuleSelectors = new()
    {
        [RateLimitPolicies.AuthLogin] = o => o.Login,
        [RateLimitPolicies.AuthRegister] = o => o.Register,
        [RateLimitPolicies.AuthRefreshToken] = o => o.RefreshToken
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

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionResponseAsync;

            foreach (var (policyName, selectRule) in RuleSelectors)
                options.AddPolicy(policyName, context => CreateIpPartition(context, selectRule));
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

        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
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
        var options = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;

        var retryAfterSeconds = ResolveRetryAfterSeconds(context, options);
        if (retryAfterSeconds is not null)
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);

        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimitingExtensions));

        logger.LogWarning("Từ chối request do vượt rate limit: {Method} {Path} từ IP {ClientIp}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            ClientIpResolver.Resolve(httpContext, options.BffSharedSecret));

        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse.Failure("RateLimit.TooManyRequests", "Bạn thao tác quá nhanh. Vui lòng thử lại sau."),
            ct);
    }

    private static int? ResolveRetryAfterSeconds(OnRejectedContext context, AuthRateLimitOptions options)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            return (int)Math.Ceiling(retryAfter.TotalSeconds);

        var policyName = context.HttpContext.GetEndpoint()?.Metadata
            .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

        if (policyName is null || !RuleSelectors.TryGetValue(policyName, out var selectRule))
            return null;

        return (int)Math.Ceiling(selectRule(options).WindowSeconds / (double)SegmentsPerWindow);
    }
}