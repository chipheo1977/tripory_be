using FluentValidation;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public class GetMessagesQueryValidator : AbstractValidator<GetMessagesQuery>
{
    public GetMessagesQueryValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty().WithMessage("Mã hội thoại không được để trống.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Số lượng tin nhắn mỗi trang từ 1 đến 100.");
    }
}
