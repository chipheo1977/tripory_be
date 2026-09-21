using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using Tripory.Domain.Enums;

namespace Tripory.Domain.Entities;

public class Conversation : EntityAuditBase<Guid>, IAggregateRoot
{
    // User1Id luôn nhỏ hơn User2Id theo thứ tự Guid để bảo đảm tính duy nhất toàn hệ thống
    public Guid User1Id { get; private set; }
    public Guid User2Id { get; private set; }

    public Guid? LastMessageId { get; private set; }
    public string? LastMessageContent { get; private set; }
    public MessageType? LastMessageType { get; private set; }
    public DateTimeOffset? LastMessageAt  { get; private set; }

    // Số tin chưa đọc được tính riêng biệt cho từng thành viên
    public int UnreadCountUser1 { get; private set; }
    public int UnreadCountUser2 { get; private set; }

    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private Conversation() { }

    public static Result<Conversation> Create(Guid userA, Guid userB)
    {
        if (userA == userB)
            return Result.Failure<Conversation>(new Error("Conversation.InvalidUser", "Định danh người dùng không hợp lệ."));

        if (userA == userB)
            return Result.Failure<Conversation>(new Error("Conversation.SelfChatNotAllowed", "Không thể tự tạo cuộc hội thoại với chính mình."));

        // Chuẩn hóa: User1Id luôn có giá trị so sánh nhỏ hơn User2Id
        var (u1, u2) = userA.CompareTo(userB) < 0 ? (userA, userB) : (userB, userA);

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            User1Id = u1,
            User2Id = u2,
            LastMessageAt = DateTimeOffset.UtcNow,
            UnreadCountUser1 = 0,
            UnreadCountUser2 = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(conversation);
    }

    // Cập nhật thông tin tóm tắt tin nhắn cuối cùng và tăng Unread Count của đối phương
    public void UpdateLastMessage(Guid messageId, MessageType type, string? content, DateTimeOffset messageAt, Guid senderId)
    {
        LastMessageId = messageId;
        LastMessageType = type;
        LastMessageContent = content;
        LastMessageAt = messageAt;
        UpdatedAt = messageAt;

        // Tăng unread count cho người nhận
        if (senderId == User1Id)
        {
            UnreadCountUser2++;
        }
        else if (senderId == User2Id)
        {
            UnreadCountUser1++;
        }
    }

    // Đánh dấu đã đọc: Đặt số lượng tin chưa đọc của người đọc về 0
    public void MarkAsRead(Guid readerId)
    {
        if (readerId == User1Id)
        {
            UnreadCountUser1 = 0;
        }
        else if (readerId == User2Id)
        {
            UnreadCountUser2 = 0;
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Tiện ích lấy Id đối phương dựa vào Id người dùng hiện tại
    public Guid GetPartnerId(Guid currentUserId)
    {
        return currentUserId == User1Id ? User2Id : User1Id;
    }

    // Lấy số lượng tin chưa đọc của người dùng hiện tại
    public int GetUnreadCount(Guid currentUserId)
    {
        return currentUserId == User1Id ? UnreadCountUser1 : UnreadCountUser2;
    }

    // Kiểm tra xem người dùng có phải là thành viên của cuộc hội thoại hay không. Sửa hàng loạt sau.
    // public bool IsParticipant(Guid userId)
    // {
    //     return userId == User1Id || userId == User2Id;
    // }
}