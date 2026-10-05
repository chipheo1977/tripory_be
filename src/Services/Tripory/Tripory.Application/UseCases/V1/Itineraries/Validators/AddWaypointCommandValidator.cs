using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Commands;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class AddWaypointCommandValidator : AbstractValidator<AddWaypointCommand>
{
    public AddWaypointCommandValidator()
    {
        RuleFor(x => x.ItineraryId)
            .NotEmpty().WithMessage("Mã hành trình không được để trống.");
        
        RuleFor(x => x.DayNumber)
            .GreaterThan(0).WithMessage("Số thứ tự ngày phải lớn hơn hoặc bằng 1.");

        this.ApplyWaypointPayloadRules();
    }
}