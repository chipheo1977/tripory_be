namespace Tripory.Application.Abstractions.Storage;

public interface IAudioStorageService
{
    Task<string> SaveAudioAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    Task DeleteAudioAsync(string fileUrl, CancellationToken ct = default);
}