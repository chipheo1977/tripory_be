using BuildingBlocks.Core.Abstractions.Shared;

namespace Tripory.Domain.Errors;

public static class DomainErrors
{
    public static class Auth
    {
        public static readonly Error Unauthorized = new(
            "Auth.Unauthorized",
            "Yêu cầu đăng nhập để thực hiện thao tác."
        );

        public static readonly Error InvalidCredentials = new(
            "Auth.InvalidCredentials",
            "Tài khoản hoặc mật khẩu không chính xác."
        );

        public static readonly Error InvalidCurrentPassword = new(
            "Auth.InvalidCurrentPassword",
            "Mật khẩu hiện tại không chính xác."
        );

        public static Error UserBanned(string? reason) => new(
            "Auth.UserBanned",
            $"Tài khoản đã bị khóa. Lý do: {reason ?? "Vi phạm chính sách."}"
        );

        public static readonly Error UserNotFound = new(
            "User.NotFound",
            "Không tìm thấy thông tin người dùng."
        );
    }

    public static class Itinerary
    {
        public static readonly Error NotFound = new(
            "Itinerary.NotFound",
            "Không tìm thấy hành trình."
        );

        public static Error NotFoundWithId(Guid id) => new(
            "Itinerary.NotFound",
            $"Không tìm thấy hành trình với mã '{id}'."
        );

        public static readonly Error Forbidden = new(
            "Itinerary.Forbidden",
            "Bạn không có quyền thao tác trên hành trình này."
        );

        public static readonly Error CannotPublishEmpty = new(
            "Itinerary.CannotPublishEmpty",
            "Không thể xuất bản hành trình khi chưa có điểm dừng chân nào."
        );

        public static readonly Error InvalidUserId = new(
            "Itinerary.InvalidUserId",
            "UserId không được để trống."
        );

        public static readonly Error InvalidDayNumber = new(
            "Itinerary.InvalidDayNumber",
            "Số thứ tự ngày phải lớn hơn hoặc bằng 1."
        );

        public static readonly Error InvalidReorderList = new(
            "Itinerary.InvalidReorderList",
            "Danh sách điểm sắp xếp không khớp với dữ liệu hiện tại."
        );
    }

    public static class Waypoint
    {
        public static readonly Error NotFound = new(
            "Waypoint.NotFound",
            "Không tìm thấy điểm dừng chân."
        );

        public static Error NotFoundWithId(Guid id) => new(
            "Waypoint.NotFound",
            $"Không tìm thấy điểm dừng chân có định danh '{id}'."
        );
    }

    public static class Chat
    {
        public static readonly Error ConversationNotFound = new(
            "Conversation.NotFound",
            "Không tìm thấy hội thoại."
        );

        public static readonly Error Forbidden = new(
            "Chat.Forbidden",
            "Bạn không thuộc cuộc trò chuyện này."
        );

        public static readonly Error SelfChat = new(
            "Conversation.SelfChat",
            "Không thể tạo cuộc trò chuyện với chính mình."
        );
    }
}
