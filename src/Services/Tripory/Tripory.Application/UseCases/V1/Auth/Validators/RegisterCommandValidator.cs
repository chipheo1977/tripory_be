// @TODO: File này chạy như thế nào? Dùng như thế nào?
using FluentValidation;
using Tripory.Application.UseCases.V1.Auth.Commands;

namespace Tripory.Application.UseCases.V1.Auth.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có tối thiểu 8 ký tự.");
            // @TODO: Giữ mật khẩu đơn giản ở giai đoạn đầu.
            // .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa.")
            // .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường.")
            // .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.")
            // .Matches(@"[\!\?\*\@\#\$\%\^\&\+\=]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt.");
    }
}
