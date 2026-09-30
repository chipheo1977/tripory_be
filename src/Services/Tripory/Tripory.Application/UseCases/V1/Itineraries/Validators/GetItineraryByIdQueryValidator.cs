using FluentValidation;
using Tripory.Application.UseCases.V1.Itineraries.Queries;

namespace Tripory.Application.UseCases.V1.Itineraries.Validators;

public class GetItineraryByIdQueryValidator : AbstractValidator<GetItineraryByIdQuery>
{
    public GetItineraryByIdQueryValidator()
    {
        RuleFor(x => x.ItineraryId)
            .NotEmpty().WithMessage("Mã hành trình không được để trống.");
    }
}
