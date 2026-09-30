using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Command;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class UpdateWaypointCommandValidator : AbstractValidator<UpdateWaypointCommand>
{
    public UpdateWaypointCommandValidator()
    {
        RuleFor(x => x.ItineraryId)
            .NotEmpty().WithMessage("Mã hành trình không được để trống.");

        RuleFor(x => x.WaypointId)
            .NotEmpty().WithMessage("Mã điểm dừng không được để trống.");

        this.ApplyWaypointPayloadRules();
    }
}
