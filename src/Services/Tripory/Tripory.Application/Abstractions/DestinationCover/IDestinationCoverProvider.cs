namespace Tripory.Application.Abstractions.DestinationCover;

public interface IDestinationCoverProvider
{
    // URL ảnh bìa mặc định của điểm đến, hoặc null khi mã không thuộc danh mục.
    string? GetDefaultCoverUrl(string? destinationCode);
}
