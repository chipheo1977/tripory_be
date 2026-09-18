# HƯỚNG DẪN TRIỂN KHAI BƯỚC 1: DOMAIN LAYER – MODULE CHAT (`Tripory.Domain`)
### Kèm Phân Tích Kiến Trúc 5W1H Để Tech Lead Tự Gõ Mã Nguồn

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 02 – Realtime Communication & User Chat System  
> **Bước thực hiện:** Bước 01 – Domain Modeling (Module Chat - Pure C#)  
> **Tiêu chuẩn tuân thủ:** Clean Architecture + DDD (Pattern 3 & 4 từ `AGENTS.md`)  
> **Tài liệu đặc tả đối chiếu:** `../tripory/docs/traveler/USER_CHAT_01` (`user_chat.md`, `PT-01`, `PT-02`, `PT-03`)

---

## 📌 MỤC LỤC TRÌNH TỰ THỰC HIỆN

1. [Tổng Quan Cấu Trúc Thư Mục Domain Chat](#1-tổng-quan-cấu-trúc-thư-mục-domain-chat)
2. [Khối 1: Các Enums Nghiệp Vụ](#-khối-1-các-enums-nghiệp-vụ)
   * 1.1 `MessageType.cs`
   * 1.2 `CallStatus.cs`
   * 1.3 `CallDirection.cs`
3. [Khối 2: Value Objects & Domain Exceptions](#-khối-2-value-objects--domain-exceptions)
   * 2.1 `CallLogData.cs`
   * 2.2 `ConversationNotFoundException.cs`
4. [Khối 3: Thực Thể Cốt Lõi (Entities & Aggregate Root)](#-khối-3-thực-thể-cốt-lõi-entities--aggregate-root)
   * 3.1 `ChatMessage.cs` (Entity bảo vệ Invariants tin nhắn văn bản, thoại, cuộc gọi)
   * 3.2 `Conversation.cs` (Aggregate Root chuẩn hóa cặp người dùng & Unread Count)
5. [Lệnh Build Kiểm Chứng (Verification)](#-lệnh-build-kiểm-chứng-verification)

---

## 1. TỔNG QUAN CẤU TRÚC THƯ MỤC DOMAIN CHAT

Các file mới sẽ được đặt gọn gàng trong project `Tripory.Domain`:

```text
src/Services/Tripory/Tripory.Domain/
├── Enums/
│   ├── MessageType.cs              # [NEW]
│   ├── CallStatus.cs               # [NEW]
│   └── CallDirection.cs            # [NEW]
├── ValueObjects/
│   └── CallLogData.cs              # [NEW]
├── Exceptions/
│   └── ConversationNotFoundException.cs # [NEW]
└── Entities/
    ├── ChatMessage.cs              # [NEW]
    └── Conversation.cs             # [NEW]
```

---

## 🏷️ KHỐI 1: CÁC ENUMS NGHIỆP VỤ

### 1.1 `MessageType.cs`
* **Why (Tại sao):** Phân loại các thể loại tin nhắn hỗ trợ trong hệ thống theo đặc tả `user_chat.md`.
* **What (Là cái gì):** C# Enum định nghĩa 4 loại: Tin nhắn văn bản (`Text`), Tin nhắn thoại (`Voice`), Thẻ lịch trình (`Itinerary` - dự phòng cho tương lai), và Nhật ký cuộc gọi (`CallLog`).
* **Who (Ai phụ trách):** Tầng `Tripory.Domain`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Domain/Enums/MessageType.cs`
* **When (Khi nào gọi):** Khi tạo tin nhắn mới, lưu trữ vào DB và client render giao diện bong bóng chat tương ứng.
* **How (Mã nguồn):**

```csharp
namespace Tripory.Domain.Enums;

public enum MessageType
{
    Text = 1,
    Voice = 2,
    Itinerary = 3, // Tạm hoãn, dành cho giai đoạn tích hợp ITINERARY_01
    CallLog = 4
}
```

---

### 1.2 `CallStatus.cs` & 1.3 `CallDirection.cs`
* **Why (Tại sao):** Phục vụ đặc tả `PT-03` ghi vết cuộc gọi (`BR_CHAT_05`). Cần lưu rõ kết quả đàm thoại (Thành công, Nhỡ, hay Bị từ chối) và hướng cuộc gọi (Gọi đến / Gọi đi).
* **Where (Vị trí file):** 
  * `src/Services/Tripory/Tripory.Domain/Enums/CallStatus.cs`
  * `src/Services/Tripory/Tripory.Domain/Enums/CallDirection.cs`
* **How (Mã nguồn):**

*Tạo file `CallStatus.cs`:*
```csharp
namespace Tripory.Domain.Enums;

public enum CallStatus
{
    Completed = 1, // Cuộc gọi hoàn thành thành công
    Missed = 2,    // Cuộc gọi nhỡ (đối phương không bắt máy)
    Declined = 3   // Cuộc gọi bị từ chối
}
```

*Tạo file `CallDirection.cs`:*
```csharp
namespace Tripory.Domain.Enums;

public enum CallDirection
{
    Inbound = 1,  // Cuộc gọi đến
    Outbound = 2  // Cuộc gọi đi
}
```

---

## 💎 KHỐI 2: VALUE OBJECTS & DOMAIN EXCEPTIONS

### 2.1 `CallLogData.cs`
* **Why (Tại sao):** Đóng gói trọn vẹn dữ liệu đàm thoại (`Status`, `DurationSeconds`, `Direction`) thành một khối bất biến (Immutable Value Object), tránh phân mảnh các cột rời rạc không cần thiết.
* **What (Là cái gì):** C# Record kế thừa Value Object concept, đại diện cho dữ liệu ghi vết cuộc gọi.
* **Who (Ai phụ trách):** Tầng `Tripory.Domain`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Domain/ValueObjects/CallLogData.cs`
* **When (Khi nào gọi):** Được nhúng vào `ChatMessage` khi thể loại tin nhắn là `CallLog`.
* **How (Mã nguồn):**

```csharp
using Tripory.Domain.Enums;

namespace Tripory.Domain.ValueObjects;

public record CallLogData(
    CallStatus Status,
    int DurationSeconds,
    CallDirection Direction
);
```

---

### 2.2 `ConversationNotFoundException.cs`
* **Why (Tại sao):** Chuẩn hóa Domain Exception khi không tìm thấy cuộc hội thoại, giúp `GlobalExceptionHandler` ở tầng API tự động bắt và trả về mã lỗi HTTP `404 Not Found`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Domain/Exceptions/ConversationNotFoundException.cs`
* **How (Mã nguồn):**

```csharp
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public class ConversationNotFoundException : NotFoundException
{
    public ConversationNotFoundException(Guid id)
        : base($"Không tìm thấy cuộc hội thoại với mã: {id}")
    {
    }
}
```

---

## 🏛️ KHỐI 3: THỰC THỂ CỐT LÕI (ENTITIES & AGGREGATE ROOT)

### 3.1 `ChatMessage.cs`
* **Why (Tại sao):** Đại diện cho một tin nhắn trong dòng thời gian chat. Entity này tự chịu trách nhiệm bảo vệ toàn bộ các ràng buộc nghiệp vụ (Invariants):
  * `BR_CHAT_02`: Tin nhắn văn bản không được rỗng, tối đa 2000 ký tự.
  * `BR_CHAT_02 (PT-02)`: Tin nhắn thoại thời lượng bắt buộc từ 1 đến 120 giây.
  * `BR_CHAT_05 (PT-03)`: Bản ghi cuộc gọi phải có `CallLogData`.
* **What (Là cái gì):** Entity kế thừa `EntityBase<Guid>`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Domain/Entities/ChatMessage.cs`
* **How (Mã nguồn):**

```csharp
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;
using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Entities;

public class ChatMessage : EntityBase<Guid>
{
    public const int MaxTextLength = 2000;
    public const int MinVoiceDurationSeconds = 1;
    public const int MaxVoiceDurationSeconds = 120;

    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public MessageType Type { get; private set; }
    public string? Content { get; private set; }
    public string? VoiceUrl { get; private set; }
    public int? VoiceDuration { get; private set; }
    public CallLogData? CallLogData { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ChatMessage() { }

    // Factory method 1: Tạo tin nhắn văn bản
    public static Result<ChatMessage> CreateText(Guid conversationId, Guid senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure<ChatMessage>(new Error("ChatMessage.EmptyContent", "Nội dung tin nhắn không được để trống."));

        var trimmedContent = content.Trim();
        if (trimmedContent.Length > MaxTextLength)
            return Result.Failure<ChatMessage>(new Error("ChatMessage.ContentTooLong", $"Tin nhắn không được vượt quá {MaxTextLength} ký tự."));

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Type = MessageType.Text,
            Content = trimmedContent,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(message);
    }

    // Factory method 2: Tạo tin nhắn thoại (PT-02)
    public static Result<ChatMessage> CreateVoice(Guid conversationId, Guid senderId, string voiceUrl, int voiceDuration)
    {
        if (string.IsNullOrWhiteSpace(voiceUrl))
            return Result.Failure<ChatMessage>(new Error("ChatMessage.InvalidVoiceUrl", "Đường dẫn file ghi âm không hợp lệ."));

        if (voiceDuration < MinVoiceDurationSeconds || voiceDuration > MaxVoiceDurationSeconds)
            return Result.Failure<ChatMessage>(new Error("ChatMessage.InvalidVoiceDuration", $"Thời lượng ghi âm phải từ {MinVoiceDurationSeconds} đến {MaxVoiceDurationSeconds} giây."));

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Type = MessageType.Voice,
            VoiceUrl = voiceUrl.Trim(),
            VoiceDuration = voiceDuration,
            Content = "🎙️ Tin nhắn thoại",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(message);
    }

    // Factory method 3: Tạo bản ghi nhật ký cuộc gọi (PT-03)
    public static Result<ChatMessage> CreateCallLog(Guid conversationId, Guid senderId, CallLogData callLogData)
    {
        if (callLogData is null)
            return Result.Failure<ChatMessage>(new Error("ChatMessage.NullCallLogData", "Dữ liệu cuộc gọi không được để trống."));

        var summaryText = callLogData.Status switch
        {
            CallStatus.Completed => $"📞 Cuộc gọi thoại ({callLogData.DurationSeconds}s)",
            CallStatus.Missed => "📞 Cuộc gọi nhỡ",
            CallStatus.Declined => "📞 Cuộc gọi bị từ chối",
            _ => "📞 Cuộc gọi thoại"
        };

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Type = MessageType.CallLog,
            CallLogData = callLogData,
            Content = summaryText,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(message);
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}
```

---

### 3.2 `Conversation.cs`
* **Why (Tại sao):** Là **Aggregate Root** quản lý phiên hội thoại giữa 2 người dùng. Để tránh việc tạo trùng lặp 2 bản ghi hội thoại đảo chiều giữa User A và User B (ví dụ: một bản ghi `(A, B)` và một bản ghi `(B, A)`), Entity này tự động **chuẩn hóa thứ tự (Normalization)**: `User1Id` luôn nhỏ hơn `User2Id`.
* **What (Là cái gì):** Entity kế thừa `EntityAuditBase<Guid>`, implement `IAggregateRoot`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Domain/Entities/Conversation.cs`
* **How (Mã nguồn):**

```csharp
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
    public DateTimeOffset LastMessageAt { get; private set; }

    // Số tin chưa đọc được tính riêng biệt cho từng thành viên
    public int UnreadCountUser1 { get; private set; }
    public int UnreadCountUser2 { get; private set; }

    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private Conversation() { }

    public static Result<Conversation> Create(Guid userA, Guid userB)
    {
        if (userA == Guid.Empty || userB == Guid.Empty)
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
}
```

---

## 🎯 LỆNH BUILD KIỂM CHỨNG (VERIFICATION)

Sau khi anh hoàn thành việc gõ các file trên, hãy chạy lệnh sau để kiểm tra tính toàn vẹn và đảm bảo tầng Domain biên dịch sạch:

```bash
dotnet build src/Services/Tripory/Tripory.Domain/Tripory.Domain.csproj
```

**Tiêu chuẩn nghiệm thu:** Lệnh trả về:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Không có bất kỳ cảnh báo hoặc lỗi biên dịch nào!
