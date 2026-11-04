using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Commands;
using Tripory.Domain.Catalogs;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class CreateQuickDraftItineraryCommandValidator : AbstractValidator<CreateQuickDraftItineraryCommand>
{
    public const int MinDayCount = 1;
    public const int MaxDayCount = 30;

    public CreateQuickDraftItineraryCommandValidator()
    {
        RuleFor(x => x.DestinationCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Vui lòng chọn điểm đến.").WithErrorCode("Itinerary.DestinationRequired")
            .Must(DestinationCatalog.IsKnown).WithMessage("Điểm đến không hợp lệ.").WithErrorCode("Itinerary.DestinationUnknown");

        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.").WithErrorCode("ItineraryTitle.Empty")
            .Must(title => (title ?? string.Empty).Trim().Length <= ItineraryTitle.MaxLength)
            .WithMessage($"Tiêu đề không được vượt quá {ItineraryTitle.MaxLength} ký tự.").WithErrorCode("ItineraryTitle.TooLong");

        RuleFor(x => x.DayCount!.Value)
            .InclusiveBetween(MinDayCount, MaxDayCount)
            .WithMessage($"Số ngày phải từ {MinDayCount} đến {MaxDayCount}.").WithErrorCode("Itinerary.DayCountOutOfRange")
            .When(x => x.DayCount.HasValue);

        RuleFor(x => x.Interests!)
            .MustBeValidInterests()
            .When(x => x.Interests is not null);
    }
}
