using FluentValidation;
using Tripory.Application.UseCases.V1.Auth.Commands;

namespace Tripory.Application.UseCases.V1.Auth.Validators;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken không được để trống.")
            .MaximumLength(512).WithMessage("RefreshToken không hợp lệ.");
    }
}
