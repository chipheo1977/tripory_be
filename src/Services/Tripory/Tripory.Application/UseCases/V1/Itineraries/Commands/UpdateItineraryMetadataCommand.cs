using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record UpdateItineraryMetadataCommand(
    Guid ItineraryId,
    string? Title,
    string? Description,
    DateOnly? StartDate,
    string? DestinationCode,
    string[]? Interests,
    string[]? CoverImages,
    int? SelectedCoverIndex
) : ICommand
{
    /// <summary>
    /// Danh sách ảnh bìa sẽ ghi (đã trim, tính một lần). Null nghĩa là giữ nguyên ảnh bìa hiện có.
    /// </summary>
    public string[]? TrimmedCoverImages { get; } = CoverImages?
        .Select(url => url?.Trim() ?? string.Empty)
        .ToArray();
}
