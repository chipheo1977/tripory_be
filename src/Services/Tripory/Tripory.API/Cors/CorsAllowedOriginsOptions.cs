namespace Tripory.API.Cors;

/// <summary>
/// Cấu hình CORS đọc từ section <c>Cors</c> (env: <c>Cors__AllowedOrigins__0</c>, <c>Cors__AllowedOrigins__1</c>...).
/// </summary>
public sealed class CorsAllowedOriginsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Danh sách origin được phép (vd: <c>https://app.example.com</c>). Rỗng/thiếu = cho phép mọi origin (chỉ nên dùng khi dev).
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}
