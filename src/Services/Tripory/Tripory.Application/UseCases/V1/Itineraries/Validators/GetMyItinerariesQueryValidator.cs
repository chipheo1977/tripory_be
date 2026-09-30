using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Queries;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class GetMyItinerariesQueryValidator : AbstractValidator<GetMyItinerariesQuery>
{
    public GetMyItinerariesQueryValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(1).WithMessage("Chỉ số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải nằm trong khoảng từ 1 đến 100.");
    }
}
