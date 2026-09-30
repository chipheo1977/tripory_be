using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Command;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public static class WaypointValidationExtensions
{
    public static void ApplyWaypointPayloadRules<T>(this AbstractValidator<T> validator)
        where T : IWaypointPayload
    {
        validator.RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên điểm dừng không được để trống.")
            .MaximumLength(200).WithMessage("Tên điểm dừng không được vượt quá 200 ký tự.");

        validator.RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Kinh độ phải nằm trong khoảng từ -180 đến 180.");

        validator.RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Vĩ độ phải nằm trong khoảng từ -90 đến 90.");
    }
}
