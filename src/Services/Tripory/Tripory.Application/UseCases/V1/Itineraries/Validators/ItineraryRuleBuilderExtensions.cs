using FluentValidation;
using Tripory.Domain.Catalogs;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public static class ItineraryRuleBuilderExtensions
{
    public const int MaxCoverImages = 5;
    public const int MaxCoverImageUrlLength = 500;

    public static IRuleBuilderOptions<T, string[]> MustBeValidInterests<T>(this IRuleBuilderInitial<T, string[]> ruleBuilder)
    {
        return ruleBuilder
            .Cascade(CascadeMode.Stop)
            .Must(interests => interests.Length <= InterestCatalog.Codes.Count)
            .WithMessage($"Chọn tối đa {InterestCatalog.Codes.Count} sở thích.").WithErrorCode("Itinerary.TooManyInterests")
            .Must(interests => interests.All(InterestCatalog.IsKnown))
            .WithMessage("Sở thích không hợp lệ.").WithErrorCode("Itinerary.InterestUnknown")
            .Must(interests => interests.Distinct(StringComparer.Ordinal).Count() == interests.Length)
            .WithMessage("Sở thích bị trùng.").WithErrorCode("Itinerary.InterestDuplicated");
    }

    public static IRuleBuilderOptions<T, string[]> MustBeValidCoverImages<T>(this IRuleBuilderInitial<T, string[]> ruleBuilder)
    {
        return ruleBuilder
            .Cascade(CascadeMode.Stop)
            .Must(urls => urls.Length <= MaxCoverImages)
            .WithMessage($"Tối đa {MaxCoverImages} ảnh bìa.").WithErrorCode("Itinerary.TooManyCoverImages")
            .Must(urls => urls.All(IsHttpsUrl))
            .WithMessage("Ảnh bìa phải là đường dẫn https hợp lệ.").WithErrorCode("Itinerary.CoverImageInvalidUrl")
            .Must(urls => urls.All(url => url.Length <= MaxCoverImageUrlLength))
            .WithMessage($"Đường dẫn ảnh bìa không được vượt quá {MaxCoverImageUrlLength} ký tự.").WithErrorCode("Itinerary.CoverImageTooLong")
            .Must(urls => urls.Distinct(StringComparer.Ordinal).Count() == urls.Length)
            .WithMessage("Ảnh bìa bị trùng.").WithErrorCode("Itinerary.CoverImageDuplicated");
    }

    private static bool IsHttpsUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (url.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && !string.IsNullOrEmpty(uri.Host);
    }
}
