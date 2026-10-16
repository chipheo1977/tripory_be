using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Tripory.API.RateLimiting;

public static class ClientIpResolver
{
    public const string BffSecretHeader = "X-Bff-Secret";
    private const string ForwardedForHeader = "X-Forwarded-For";

    public static string Resolve(HttpContext context, string bffSharedSecret)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (string.IsNullOrEmpty(bffSharedSecret) || !IsTrustedBff(context, bffSharedSecret))
            return remoteIp;

        var clientIp = context.Request.Headers[ForwardedForHeader].ToString()
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return IPAddress.TryParse(clientIp, out var parsed) ? parsed.ToString() : remoteIp;
    }

    private static bool IsTrustedBff(HttpContext context, string expectedSecret)
    {
        var provided = context.Request.Headers[BffSecretHeader].ToString();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expectedSecret));
    }
}