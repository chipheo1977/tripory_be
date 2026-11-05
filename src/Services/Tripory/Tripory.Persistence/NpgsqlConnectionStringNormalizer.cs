using Npgsql;

namespace Tripory.Persistence;

/// <summary>
/// Chuẩn hóa chuỗi kết nối: nhận cả dạng key=value lẫn URL postgresql:// (định dạng Neon cấp).
/// Chuỗi dạng key=value được giữ nguyên để docker-compose cục bộ không bị ảnh hưởng.
/// </summary>
internal static class NpgsqlConnectionStringNormalizer
{
    public static string Normalize(string connectionString)
    {
        if (!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'))
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
            {
                builder.Password = Uri.UnescapeDataString(parts[1]);
            }
        }

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
            switch (key.ToLowerInvariant())
            {
                case "sslmode":
                    builder.SslMode = Enum.Parse<SslMode>(value.Replace("-", string.Empty), ignoreCase: true);
                    break;
                case "channel_binding":
                    builder.ChannelBinding = Enum.Parse<ChannelBinding>(value, ignoreCase: true);
                    break;
                default:
                    // Tham số lạ: để Npgsql tự kiểm tra, không bỏ qua âm thầm.
                    builder[key] = value;
                    break;
            }
        }

        return builder.ConnectionString;
    }
}
