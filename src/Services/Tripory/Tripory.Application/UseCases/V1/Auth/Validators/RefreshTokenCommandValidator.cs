using FluentValidation;
using Tripory.Application.UseCases.V1.Auth.Commands;

namespace Tripory.Application.UseCases.V1.Auth.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty().WithMessage("AccessToken không được để trống.");
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("RefreshToken không được để trống.");
    }
}
