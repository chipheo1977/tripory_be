using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Command;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class SetDaySubtitleCommandValidator : AbstractValidator<SetDaySubtitleCommand>
{
    public SetDaySubtitleCommandValidator()
    {
        RuleFor(x => x.ItineraryId)
            .NotEmpty().WithMessage("Mã hành trình không được để trống.");

        RuleFor(x => x.DayNumber)
            .GreaterThan(0).WithMessage("Số thứ tự ngày phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.Subtitle)
            .MaximumLength(100).WithMessage("Tiêu đề phụ không được vượt quá 100 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Subtitle));
    }
}
