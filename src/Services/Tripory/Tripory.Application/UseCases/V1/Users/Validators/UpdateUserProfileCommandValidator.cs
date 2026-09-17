using FluentValidation;
using Tripory.Application.UseCases.V1.Users.Commands;

namespace Tripory.Application.UseCases.V1.Users.Validators;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

        RuleFor(x => x.Bio)
            .MaximumLength(250).WithMessage("Tiểu sử không được vượt quá 250 ký tự.");
    }
}

