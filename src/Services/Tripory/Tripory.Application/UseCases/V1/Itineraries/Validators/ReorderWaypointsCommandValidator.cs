using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Commands;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class ReorderWaypointsCommandValidator : AbstractValidator<ReorderWaypointsCommand>
{
    public ReorderWaypointsCommandValidator()
    {
        RuleFor(x => x.ItineraryId)
            .NotEmpty().WithMessage("Mã hành trình không được để trống.");

        RuleFor(x => x.DayNumber)
            .GreaterThan(0).WithMessage("Số thứ tự ngày phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.OrderedWaypointIds)
            .NotEmpty().WithMessage("Danh sách thứ tự điểm dừng không được để trống.");
    }
}
