namespace Tripory.API.Cors;

public static class CorsExtensions
{
    public const string PolicyName = "TriporyCors";

    /// <summary>
    /// Đăng ký CORS policy từ cấu hình <c>Cors:AllowedOrigins</c> và trả về danh sách origin đã chuẩn hóa.
    /// Danh sách không rỗng: chỉ cho phép đúng các origin đó (kèm AllowCredentials, không bao giờ dùng wildcard).
    /// Danh sách rỗng/thiếu: giữ hành vi cũ (cho phép mọi origin) để không làm hỏng môi trường hiện có.
    /// </summary>
    /// <exception cref="InvalidOperationException">Có origin không phải URL http/https tuyệt đối.</exception>
    public static string[] AddTriporyCors(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(CorsAllowedOriginsOptions.SectionName)
            .Get<CorsAllowedOriginsOptions>() ?? new CorsAllowedOriginsOptions();

        var origins = NormalizeOrigins(options.AllowedOrigins);

        services.AddCors(cors =>
        {
            cors.AddPolicy(PolicyName, policy =>
            {
                if (origins.Length > 0)
                {
                    // SignalR (negotiate + WebSocket) dùng cookie/credentials nên bắt buộc AllowCredentials,
                    // vì vậy KHÔNG được kết hợp với wildcard origin.
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
            });
        });

        return origins;
    }

    /// <summary>
    /// Ghi cảnh báo khi CORS đang mở cho mọi origin ngoài môi trường Development.
    /// </summary>
    public static void WarnIfCorsIsOpen(this WebApplication app, string[] configuredOrigins)
    {
        if (configuredOrigins.Length > 0 || app.Environment.IsDevelopment())
            return;

        app.Logger.LogWarning(
            "CORS đang mở cho MỌI origin vì chưa cấu hình '{ConfigKey}'. " +
            "Hãy cấu hình danh sách origin (env: Cors__AllowedOrigins__0, Cors__AllowedOrigins__1, ...) trước khi triển khai thật.",
            $"{CorsAllowedOriginsOptions.SectionName}:AllowedOrigins");
    }

    private static string[] NormalizeOrigins(IEnumerable<string>? rawOrigins)
    {
        var result = new List<string>();

        foreach (var raw in rawOrigins ?? [])
        {
            // Bỏ qua phần tử rỗng (vd: biến môi trường được khai báo nhưng để trống).
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var origin = raw.Trim().TrimEnd('/');

            var isValid = Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && !string.IsNullOrEmpty(uri.Host)
                && uri.AbsolutePath == "/"
                && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment)
                && string.IsNullOrEmpty(uri.UserInfo);

            if (!isValid)
            {
                throw new InvalidOperationException(
                    $"Cors:AllowedOrigins chứa giá trị không hợp lệ '{raw}'. " +
                    "Mỗi origin phải là URL tuyệt đối http/https, chỉ gồm scheme + host (+ port), " +
                    "vd: https://app.example.com. Không dùng wildcard '*'.");
            }

            if (!result.Contains(origin, StringComparer.OrdinalIgnoreCase))
                result.Add(origin);
        }

        return [.. result];
    }
}
