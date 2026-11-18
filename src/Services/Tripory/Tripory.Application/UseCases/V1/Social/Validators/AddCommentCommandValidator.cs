using FluentValidation;
using Tripory.Application.UseCases.V1.Social.Commands;
using Tripory.Domain.Entities;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Social.Validators;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.Content)
            .Cascade(CascadeMode.Stop)
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .WithMessage(DomainErrors.Comment.ContentRequired.Message)
            .WithErrorCode(DomainErrors.Comment.ContentRequired.Code)
            .Must(content => content!.Trim().Length <= ItineraryComment.ContentMaxLength)
            .WithMessage(DomainErrors.Comment.ContentTooLong.Message)
            .WithErrorCode(DomainErrors.Comment.ContentTooLong.Code);
    }
}