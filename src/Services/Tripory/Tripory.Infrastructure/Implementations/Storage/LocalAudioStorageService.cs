using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tripory.Application.Abstractions.Storage;

namespace Tripory.Infrastructure.Implementations.Storage;

public class LocalAudioStorageService : IAudioStorageService
{
    private const string UploadFolder = "uploads/voices";
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LocalAudioStorageService> _logger;

    public LocalAudioStorageService(
        IWebHostEnvironment enviroment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LocalAudioStorageService> logger
    )
    {
        _environment = enviroment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<string> SaveAudioAsync(
        Stream fileStream, 
        string fileName, 
        string contentType, 
        CancellationToken ct = default
    )
    {
        var targetDirectory = EnsureUploadDirectoryExists();
        var extension = ResolveFileExtension(fileName, contentType);
        var uniqueFileName = GenerateUniqueFileName(extension);
        var fullPath = Path.Combine(targetDirectory, uniqueFileName);

        await WriteFileToDiskAsync(fileStream, fullPath, ct);
        _logger.LogInformation("Đã lưu file âm thanh tại: {Path}", fullPath);

        return BuildPublicUrl(uniqueFileName);
    }

    public Task DeleteAudioAsync(string fileUrl, CancellationToken ct = default)
    {
        try
        {
            var fullPath = ResolveLocalFilePath(fileUrl);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Đã xóa file âm thanh tại: {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể xóa file âm thanh tại URL: {Url}", fileUrl);
        }

        return Task.CompletedTask;
    }

    #region Private Helper Methods

    /// <summary>
    /// Đảm bảo thư mục lưu trữ uploads/voices tồn tại trên hệ thống file.
    /// </summary>
    private string EnsureUploadDirectoryExists()
    {
        var rootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        var targetDirectory = Path.Combine(rootPath, UploadFolder);
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        return targetDirectory;
    }

    /// <summary>
    /// Xác định phần mở rộng của file dựa trên tên file hoặc MIME content type.
    /// </summary>
    private static string ResolveFileExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension;
        }

        return contentType switch
        {
            "audio/webm" => ".webm",
            "audio/wav" or "audio/x-wav" => ".wav",
            "audio/ogg" => ".ogg",
            "audio/mp3" or "audio/mpeg" => ".mp3",
            _ => ".webm"
        };
    }

    /// <summary>
    /// Sinh tên file duy nhất theo định dạng quy chuẩn PT-02 §6: voice_{timestamp}_{uuid}{ext}
    /// </summary>
    private static string GenerateUniqueFileName(string extension)
    {
        return $"voice_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid()}{extension}";
    }

    /// <summary>
    /// Ghi luồng file stream xuống ổ đĩa bất đồng bộ.
    /// </summary>
    private static async Task WriteFileToDiskAsync(Stream fileStream, string destinationPath, CancellationToken ct)
    {
        await using var outputStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            useAsync: true
        );

        await fileStream.CopyToAsync(outputStream, ct);
    }

    /// <summary>
    /// Sinh URL truy cập công khai bao gồm cả Scheme và Host từ HttpContext nếu có.
    /// </summary>
    private string BuildPublicUrl(string uniqueFileName)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null)
        {
            return $"{request.Scheme}://{request.Host}/{UploadFolder}/{uniqueFileName}";
        }

        return $"/{UploadFolder}/{uniqueFileName}";
    }

    /// <summary>
    /// Chuyển đổi URL tương đối hoặc tuyệt đối thành đường dẫn vật lý trên ổ cứng server.
    /// </summary>
    private string ResolveLocalFilePath(string fileUrl)
    {
        var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
        var relativePath = uri.IsAbsoluteUri ? uri.AbsolutePath.TrimStart('/') : fileUrl.TrimStart('/');

        var rootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        return Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    #endregion
}
