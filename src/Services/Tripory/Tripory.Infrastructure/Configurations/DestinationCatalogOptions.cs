using Tripory.Application.UseCases.V1.Itineraries.Validators;
using Tripory.Domain.Catalogs;

namespace Tripory.Infrastructure.Configurations;

public class DestinationCatalogOptions
{
    public const string SectionName = "DestinationCatalog";
    private const string CoverImageExtension = ".jpg";

    public string CoverImageBaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Độ dài tối đa của gốc URL để URL ghép ra (gốc + slug dài nhất + ".jpg")
    /// vẫn nằm trong giới hạn độ dài URL ảnh bìa.
    /// </summary>
    public static int MaxBaseUrlLength =>
        ItineraryRuleBuilderExtensions.MaxCoverImageUrlLength
        - DestinationCatalog.Codes.Max(code => code.Length)
        - CoverImageExtension.Length;

    public static bool IsAbsoluteHttpsUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            return false;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && !string.IsNullOrEmpty(uri.Host)
               && string.IsNullOrEmpty(uri.Query)
               && string.IsNullOrEmpty(uri.Fragment)
               && !value.Contains('?')
               && !value.Contains('#');
    }
}
