using FluentValidation;
using Tripory.Application.UseCases.V1.Social.Queries;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Social.Validators;

public class GetCommentsQueryValidator : AbstractValidator<GetCommentsQuery>
{
    public const int MaxPageSize = 50;

    public GetCommentsQueryValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(1)
            .WithMessage(DomainErrors.Comment.PageInvalid.Message)
            .WithErrorCode(DomainErrors.Comment.PageInvalid.Code);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage(DomainErrors.Comment.PageInvalid.Message)
            .WithErrorCode(DomainErrors.Comment.PageInvalid.Code);
    }
}