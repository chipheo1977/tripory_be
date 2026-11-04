using Microsoft.Extensions.Options;
using Tripory.Application.Abstractions.DestinationCover;
using Tripory.Domain.Catalogs;
using Tripory.Infrastructure.Configurations;

namespace Tripory.Infrastructure.Implementations.DestinationCover;

public class DestinationCoverProvider : IDestinationCoverProvider
{
    private const string CoverFileExtension = ".jpg";

    private readonly DestinationCatalogOptions _options;

    public DestinationCoverProvider(IOptions<DestinationCatalogOptions> options)
    {
        _options = options.Value;
    }

    public string? GetDefaultCoverUrl(string? destinationCode)
    {
        if (!DestinationCatalog.IsKnown(destinationCode))
            return null;

        return $"{_options.CoverImageBaseUrl}{destinationCode}{CoverFileExtension}";
    }
}
