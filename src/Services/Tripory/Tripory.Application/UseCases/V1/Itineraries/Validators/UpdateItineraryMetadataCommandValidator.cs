using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Command;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class UpdateItineraryMetadataCommandValidator : AbstractValidator<UpdateItineraryMetadataCommand>
{
    public UpdateItineraryMetadataCommandValidator()
    {
        RuleFor(x => x.ItineraryId)
            .NotEmpty().WithMessage("Mã hành trình không được để trống.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.")
            .MinimumLength(1).WithMessage("Tiêu đề phải có ít nhất 1 ký tự.")
            .MaximumLength(100).WithMessage("Tiêu đề không được vượt quá 100 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.");
    }
}