using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Commands;
using Tripory.Domain.Catalogs;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class UpdateItineraryMetadataCommandValidator : AbstractValidator<UpdateItineraryMetadataCommand>
{
    public UpdateItineraryMetadataCommandValidator()
    {
        RuleFor(x => x.ItineraryId)
            .NotEmpty().WithMessage("Mã hành trình không được để trống.").WithErrorCode("Itinerary.InvalidId");

        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.").WithErrorCode("ItineraryTitle.Empty")
            .Must(title => (title ?? string.Empty).Trim().Length <= ItineraryTitle.MaxLength)
            .WithMessage($"Tiêu đề không được vượt quá {ItineraryTitle.MaxLength} ký tự.").WithErrorCode("ItineraryTitle.TooLong");

        RuleFor(x => x.Description!)
            .Must(description => description.Trim().Length <= ItineraryDescription.MaxLength)
            .WithMessage($"Mô tả không được vượt quá {ItineraryDescription.MaxLength} ký tự.").WithErrorCode("ItineraryDescription.TooLong")
            .When(x => x.Description is not null);

        RuleFor(x => x.DestinationCode!)
            .Must(DestinationCatalog.IsKnown)
            .WithMessage("Điểm đến không hợp lệ.").WithErrorCode("Itinerary.DestinationUnknown")
            .When(x => x.DestinationCode is not null);

        RuleFor(x => x.Interests!)
            .MustBeValidInterests()
            .When(x => x.Interests is not null);

        RuleFor(x => x.TrimmedCoverImages!)
            .MustBeValidCoverImages()
            .When(x => x.TrimmedCoverImages is not null);

        RuleFor(x => x)
            .Must(HaveConsistentCoverSelection)
            .WithMessage("Ảnh bìa đang chọn không hợp lệ.").WithErrorCode("Itinerary.CoverSelectionInvalid")
            .When(x => x.TrimmedCoverImages is not null);
    }

    private static bool HaveConsistentCoverSelection(UpdateItineraryMetadataCommand command)
    {
        var imageCount = command.TrimmedCoverImages!.Length;
        var selectedIndex = command.SelectedCoverIndex;

        if (imageCount == 0)
            return selectedIndex is null;

        return selectedIndex is >= 0 && selectedIndex < imageCount;
    }
}
