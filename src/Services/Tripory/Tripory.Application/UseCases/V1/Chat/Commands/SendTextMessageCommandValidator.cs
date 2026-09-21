using FluentValidation;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class SendTextMessageCommandValidator : AbstractValidator<SendTextMessageCommand>
{
    public SendTextMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty().WithMessage("Mã hội thoại không được để trống.");
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung tin nhắn không được để trống.")
            .MaximumLength(2000).WithMessage("Tin nhắn không được vượt quá 2000 ký tự.");
    }
}
