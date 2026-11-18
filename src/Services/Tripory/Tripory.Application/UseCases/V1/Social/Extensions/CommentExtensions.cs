using Tripory.Application.Abstractions.Data;
using Tripory.Application.UseCases.V1.Social.Responses;
using Tripory.Domain.Entities;

namespace Tripory.Application.UseCases.V1.Social.Extensions;

public static class CommentExtensions
{
    /// <summary>
    /// Tên hiển thị: FullName; rỗng hoặc toàn khoảng trắng thì dùng Handle (hợp đồng mục 5).
    /// Không trả email, vai trò, trạng thái.
    /// </summary>
    public static CommentAuthorDto ToAuthorDto(this UserSummary user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName)
            ? user.Handle.Value
            : user.FullName;

        return new CommentAuthorDto(user.Id, displayName, user.AvatarUrl);
    }

    /// <summary>
    /// author = null khi người dùng không còn tồn tại (hợp đồng mục 5).
    /// </summary>
    public static CommentDto ToDto(this ItineraryComment comment, UserSummary? author)
    {
        return new CommentDto(
            comment.Id,
            comment.Content,
            comment.CreatedAt,
            author?.ToAuthorDto()
        );
    }
}
