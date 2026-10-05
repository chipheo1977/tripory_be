using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Commands;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class CreateQuickDraftItineraryCommandValidator : AbstractValidator<CreateQuickDraftItineraryCommand>
{
    public CreateQuickDraftItineraryCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.")
            .MinimumLength(1).WithMessage("Tiêu đề phải có ít nhất 1 ký tự.")
            .MaximumLength(100).WithMessage("Tiêu đề không được vượt quá 100 ký tự.");
    }
}