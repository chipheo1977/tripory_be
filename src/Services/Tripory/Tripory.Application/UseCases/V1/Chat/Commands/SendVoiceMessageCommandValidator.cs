using FluentValidation;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class SendVoiceMessageCommandValidator : AbstractValidator<SendVoiceMessageCommand>
{
    public SendVoiceMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty().WithMessage("Mã hội thoại không được để trống.");
        RuleFor(x => x.VoiceUrl).NotEmpty().WithMessage("Đường dẫn file ghi âm không được để trống.");
        RuleFor(x => x.VoiceDuration)
            .InclusiveBetween(1, 120)
            .WithMessage("Thời lượng ghi âm phải từ 1 đến 120 giây (PT-02).");
    }
}
