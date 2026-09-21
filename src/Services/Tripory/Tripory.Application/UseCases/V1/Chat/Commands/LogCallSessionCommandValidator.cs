using FluentValidation;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class LogCallSessionCommandValidator : AbstractValidator<LogCallSessionCommand>
{
    public LogCallSessionCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty().WithMessage("Mã hội thoại không được để trống.");
        RuleFor(x => x.DurationSeconds).GreaterThanOrEqualTo(0).WithMessage("Thời lượng cuộc gọi không được âm.");
    }
}
