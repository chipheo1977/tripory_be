# HƯỚNG DẪN TRIỂN KHAI BƯỚC 2: APPLICATION LAYER – MODULE CHAT (`Tripory.Application`)
### Kèm Phân Tích Kiến Trúc 5W1H Để Tech Lead Tự Gõ Mã Nguồn

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 02 – Realtime Communication & User Chat System  
> **Bước thực hiện:** Bước 02 – Application Layer (CQRS UseCases, SignalR Ports & DTOs)  
> **Tiêu chuẩn tuân thủ:** Clean Architecture + DDD + CQRS (Pattern 1 & Pattern 2 từ `AGENTS.md`)  
> **Tài liệu đặc tả đối chiếu:** `../tripory/docs/traveler/USER_CHAT_01` (`PT-01`, `PT-02`, `PT-03`)

---

## 📌 MỤC LỤC TRÌNH TỰ THỰC HIỆN

1. [Tổng Quan Cấu Trúc Thư Mục](#1-tổng-quan-cấu-trúc-thư-mục)
2. [Khối 1: Các Cổng Trừu Tượng (Ports / DIP)](#-khối-1-các-cổng-trừu-tượng-ports--dip)
   * 1.1 `IConversationRepository.cs`
   * 1.2 `IChatMessageRepository.cs`
   * 1.3 `IChatNotificationService.cs` (SignalR Port)
3. [Khối 2: DTOs & Responses](#-khối-2-dtos--responses)
   * 2.1 `ParticipantDto.cs`
   * 2.2 `ChatMessageDto.cs`
   * 2.3 `ConversationDto.cs`
4. [Khối 3: Vertical Feature Slice – Queries (Truy Vấn Lịch Sử Chat)](#-khối-3-vertical-feature-slice--queries-truy-vấn-lịch-sử-chat)
   * 3.1 `GetConversationsQuery` (Danh sách hội thoại của tôi)
   * 3.2 `GetMessagesQuery` (Phân trang tin nhắn hội thoại)
5. [Khối 4: Vertical Feature Slice – Commands (Thao Tác Gửi & Nhận)](#-khối-4-vertical-feature-slice--commands-thao-tác-gửi--nhận)
   * 4.1 `GetOrCreateConversationCommand` (Khởi tạo hoặc lấy hội thoại 1-1)
   * 4.2 `SendTextMessageCommand` (Gửi tin nhắn văn bản)
   * 4.3 `SendVoiceMessageCommand` (Gửi tin nhắn thoại rảnh tay 120s)
   * 4.4 `MarkConversationAsReadCommand` (Đánh dấu đã xem toàn bộ tin nhắn)
   * 4.5 `LogCallSessionCommand` (Ghi nhận kết quả cuộc gọi thoại vào đoạn chat)
6. [Lệnh Build Kiểm Chứng (Verification)](#-lệnh-build-kiểm-chứng-verification)

---

## 1. TỔNG QUAN CẤU TRÚC THƯ MỤC

```text
src/Services/Tripory/Tripory.Application/
├── Abstractions/
│   ├── Data/
│   │   ├── IConversationRepository.cs   # [NEW]
│   │   └── IChatMessageRepository.cs     # [NEW]
│   └── Realtime/
│       └── IChatNotificationService.cs   # [NEW]
└── UseCases/V1/Chat/
    ├── Responses/
    │   ├── ParticipantDto.cs             # [NEW]
    │   ├── ChatMessageDto.cs             # [NEW]
    │   └── ConversationDto.cs            # [NEW]
    ├── Queries/
    │   ├── GetConversationsQuery.cs      # [NEW]
    │   ├── GetConversationsQueryHandler.cs # [NEW]
    │   ├── GetMessagesQuery.cs           # [NEW]
    │   ├── GetMessagesQueryValidator.cs  # [NEW]
    │   └── GetMessagesQueryHandler.cs    # [NEW]
    └── Commands/
        ├── GetOrCreateConversationCommand.cs        # [NEW]
        ├── GetOrCreateConversationCommandHandler.cs  # [NEW]
        ├── SendTextMessageCommand.cs                # [NEW]
        ├── SendTextMessageCommandValidator.cs       # [NEW]
        ├── SendTextMessageCommandHandler.cs         # [NEW]
        ├── SendVoiceMessageCommand.cs               # [NEW]
        ├── SendVoiceMessageCommandValidator.cs      # [NEW]
        ├── SendVoiceMessageCommandHandler.cs        # [NEW]
        ├── MarkConversationAsReadCommand.cs         # [NEW]
        ├── MarkConversationAsReadCommandHandler.cs  # [NEW]
        ├── LogCallSessionCommand.cs                 # [NEW]
        ├── LogCallSessionCommandValidator.cs        # [NEW]
        └── LogCallSessionCommandHandler.cs          # [NEW]
```

---

## 🏛️ KHỐI 1: CÁC CỔNG TRỪU TƯỢNG (PORTS / DIP)

### 1.1 `IConversationRepository.cs`
* **Why (Tại sao):** Tuân thủ Dependency Inversion Principle (DIP). Handlers cần thao tác với Aggregate Root `Conversation` nhưng không được dính dáng trực tiếp tới `DbContext` hay các thư viện ORM cụ thể của Persistence.
* **What (Là cái gì):** Interface định nghĩa các truy vấn và lưu trữ hội thoại.
* **Who (Ai phụ trách):** Tầng `Application` định nghĩa; Tầng `Persistence` sẽ triển khai.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Abstractions/Data/IConversationRepository.cs`
* **When (Khi nào gọi):** Khi tìm kiếm hội thoại giữa 2 người dùng, lấy danh sách hội thoại, hoặc lưu trạng thái tin nhắn cuối cùng.
* **How (Mã nguồn):**

```csharp
using Tripory.Domain.Entities;

namespace Tripory.Application.Abstractions.Data;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Conversation?> GetByUsersAsync(Guid userA, Guid userB, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task UpdateAsync(Conversation conversation, CancellationToken ct = default);
}
```

---

### 1.2 `IChatMessageRepository.cs`
* **Why (Tại sao):** Tách biệt việc truy vấn lịch sử tin nhắn và đếm số tin chưa đọc khỏi entity logic.
* **What (Là cái gì):** Interface quản lý thêm mới và phân trang tin nhắn chat.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Abstractions/Data/IChatMessageRepository.cs`
* **How (Mã nguồn):**

```csharp
using Tripory.Domain.Entities;

namespace Tripory.Application.Abstractions.Data;

public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessage>> GetMessagesByConversationAsync(Guid conversationId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(ChatMessage message, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessage>> GetUnreadMessagesAsync(Guid conversationId, Guid receiverId, CancellationToken ct = default);
}
```

---

### 1.3 `IChatNotificationService.cs` (SignalR Port)
* **Why (Tại sao):** Khi người dùng gửi tin nhắn, hệ thống cần đẩy thông báo thời gian thực đến người nhận ngay lập tức qua WebSocket/SignalR. Để tầng `Application` hoàn toàn thuần túy và không bị ép cài đặt package `Microsoft.AspNetCore.SignalR`, ta định nghĩa một Port trừu tượng tại đây.
* **What (Là cái gì):** Interface cung cấp các hàm phát sự kiện realtime xuống client.
* **Who (Ai phụ trách):** Tầng `Application` sở hữu interface; Tầng `Infrastructure` sẽ hiện thực hóa bằng `IHubContext<ChatHub>`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Abstractions/Realtime/IChatNotificationService.cs`
* **When (Khi nào gọi):** Ngay sau khi Handler lưu tin nhắn thành công vào cơ sở dữ liệu.
* **How (Mã nguồn):**

```csharp
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.Abstractions.Realtime;

public interface IChatNotificationService
{
    // Đẩy tin nhắn mới đến client người nhận
    Task SendMessageNotificationAsync(Guid recipientId, ChatMessageDto message, CancellationToken ct = default);

    // Thông báo cho người gửi biết đối phương đã xem tin nhắn (Read Receipt)
    Task SendMessageReadNotificationAsync(Guid senderId, Guid conversationId, Guid readerId, CancellationToken ct = default);

    // Cập nhật lại danh sách hội thoại của người nhận (cập nhật tin cuối, tăng unread count)
    Task SendConversationUpdatedNotificationAsync(Guid recipientId, ConversationDto conversation, CancellationToken ct = default);
}
```

---

## 📦 KHỐI 2: DTOS & RESPONSES

### 2.1 `ParticipantDto.cs`
* **Why (Tại sao):** Hiển thị thông tin đối phương (Tên, Handle, Avatar) trong danh sách chat mà không làm lộ các dữ liệu nhạy cảm của bảng `users`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Responses/ParticipantDto.cs`
* **How (Mã nguồn):**

```csharp
namespace Tripory.Application.UseCases.V1.Chat.Responses;

public record ParticipantDto(
    Guid Id,
    string FullName,
    string Handle,
    string AvatarUrl
);
```

---

### 2.2 `ChatMessageDto.cs`
* **Why (Tại sao):** Chuẩn hóa cấu trúc tin nhắn trả về cho cả REST API và gói tin WebSocket SignalR.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Responses/ChatMessageDto.cs`
* **How (Mã nguồn):**

```csharp
using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Chat.Responses;

public record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    MessageType Type,
    string? Content,
    string? VoiceUrl,
    int? VoiceDuration,
    CallLogData? CallLogData,
    bool IsRead,
    DateTimeOffset CreatedAt
);
```

---

### 2.3 `ConversationDto.cs`
* **Why (Tại sao):** Đại diện cho một dòng hội thoại trong danh sách `/messages` gồm: Thông tin bạn chat (`Partner`), tin nhắn xem trước gần nhất (`LastMessage`), số tin chưa đọc (`UnreadCount`), và thời điểm cập nhật.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Responses/ConversationDto.cs`
* **How (Mã nguồn):**

```csharp
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Chat.Responses;

public record ConversationDto(
    Guid Id,
    ParticipantDto Partner,
    string? LastMessageContent,
    MessageType? LastMessageType,
    DateTimeOffset? LastMessageAt,
    int UnreadCount,
    DateTimeOffset UpdatedAt
);
```

---

## 🔍 KHỐI 3: VERTICAL FEATURE SLICE – QUERIES (TRUY VẤN LỊCH SỬ CHAT)

### 3.1 `GetConversationsQuery` (Danh Sách Hội Thoại Của Tôi)

#### File 1: Query Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Queries/GetConversationsQuery.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public record GetConversationsQuery : IQuery<IReadOnlyList<ConversationDto>>;
```

#### File 2: Query Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Queries/GetConversationsQueryHandler.cs`
* **Why:** Trích xuất `current_user` $\rightarrow$ Lấy tất cả hội thoại của user $\rightarrow$ Đọc thông tin đối phương từ `IUserRepository` $\rightarrow$ Map sang `ConversationDto`.
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Queries;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public class GetConversationsQueryHandler : IQueryHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationsQueryHandler(
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<ConversationDto>>> Handle(GetConversationsQuery request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<IReadOnlyList<ConversationDto>>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;
        var conversations = await _conversationRepository.GetUserConversationsAsync(currentUserId, ct);

        var dtos = new List<ConversationDto>();

        foreach (var conv in conversations)
        {
            var partnerId = conv.GetPartnerId(currentUserId);
            var partner = await _userRepository.GetByIdAsync(partnerId, ct);

            var partnerDto = new ParticipantDto(
                partnerId,
                partner?.FullName ?? "Người dùng ẩn danh",
                partner?.Handle.Value ?? "@user",
                partner?.AvatarUrl ?? "https://api.dicebear.com/7.x/identicon/svg?seed=tripory"
            );

            dtos.Add(new ConversationDto(
                conv.Id,
                partnerDto,
                conv.LastMessageContent,
                conv.LastMessageType,
                conv.LastMessageAt,
                conv.GetUnreadCount(currentUserId),
                conv.UpdatedAt
            ));
        }

        return Result.Success<IReadOnlyList<ConversationDto>>(dtos);
    }
}
```

---

### 3.2 `GetMessagesQuery` (Phân Trang Lịch Sử Tin Nhắn)

#### File 1: Query Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Queries/GetMessagesQuery.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public record GetMessagesQuery(
    Guid ConversationId,
    int Page = 1,
    int PageSize = 30
) : IQuery<IReadOnlyList<ChatMessageDto>>;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Queries/GetMessagesQueryValidator.cs`
* **How:**

```csharp
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
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Queries/GetMessagesQueryHandler.cs`
* **Why:** Kiểm tra quyền truy cập hội thoại (User hiện tại có phải thành viên của hội thoại không) $\rightarrow$ Đọc lịch sử tin nhắn sắp xếp theo thời gian $\rightarrow$ Trả về DTOs.
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public class GetMessagesQueryHandler : IQueryHandler<GetMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMessagesQueryHandler(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(GetMessagesQuery request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>(new Error("Conversation.NotFound", "Không tìm thấy hội thoại."));

        // Bảo vệ bảo mật: Người dùng phải là một trong 2 thành viên
        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>(new Error("Chat.Forbidden", "Bạn không có quyền truy cập vào đoạn chat này."));

        var messages = await _chatMessageRepository.GetMessagesByConversationAsync(
            request.ConversationId, request.Page, request.PageSize, ct);

        var dtos = messages.Select(m => new ChatMessageDto(
            m.Id,
            m.ConversationId,
            m.SenderId,
            m.Type,
            m.Content,
            m.VoiceUrl,
            m.VoiceDuration,
            m.CallLogData,
            m.IsRead,
            m.CreatedAt
        )).ToList();

        return Result.Success<IReadOnlyList<ChatMessageDto>>(dtos);
    }
}
```

---

## ⚡ KHỐI 4: VERTICAL FEATURE SLICE – COMMANDS (THAO TÁC GỬI & NHẬN)

### 4.1 `GetOrCreateConversationCommand` (Khởi Tạo Hoặc Mở Hội Thoại 1-1)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/GetOrCreateConversationCommand.cs`
* **Why:** Phục vụ các Entry Points (Bấm nút "Nhắn tin" từ trang Profile thành viên): Tìm xem đã có hội thoại giữa 2 người chưa; nếu có thì trả về, nếu chưa thì tự động tạo mới.
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record GetOrCreateConversationCommand(Guid PartnerId) : ICommand<ConversationDto>;
```

#### File 2: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/GetOrCreateConversationCommandHandler.cs`
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class GetOrCreateConversationCommandHandler : ICommandHandler<GetOrCreateConversationCommand, ConversationDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetOrCreateConversationCommandHandler(
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ConversationDto>> Handle(GetOrCreateConversationCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ConversationDto>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        if (currentUserId == request.PartnerId)
            return Result.Failure<ConversationDto>(new Error("Conversation.SelfChat", "Không thể tạo cuộc trò chuyện với chính mình."));

        var partner = await _userRepository.GetByIdAsync(request.PartnerId, ct);
        if (partner is null)
            return Result.Failure<ConversationDto>(new Error("User.NotFound", "Không tìm thấy người dùng này."));

        // Tìm hội thoại hiện có
        var conversation = await _conversationRepository.GetByUsersAsync(currentUserId, request.PartnerId, ct);

        if (conversation is null)
        {
            var createResult = Conversation.Create(currentUserId, request.PartnerId);
            if (createResult.IsFailure)
                return Result.Failure<ConversationDto>(createResult.Error);

            conversation = createResult.Value;
            await _conversationRepository.AddAsync(conversation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var partnerDto = new ParticipantDto(
            partner.Id,
            partner.FullName,
            partner.Handle.Value,
            partner.AvatarUrl
        );

        return Result.Success(new ConversationDto(
            conversation.Id,
            partnerDto,
            conversation.LastMessageContent,
            conversation.LastMessageType,
            conversation.LastMessageAt,
            conversation.GetUnreadCount(currentUserId),
            conversation.UpdatedAt
        ));
    }
}
```

---

### 4.2 `SendTextMessageCommand` (Gửi Tin Nhắn Văn Bản)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/SendTextMessageCommand.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record SendTextMessageCommand(
    Guid ConversationId,
    string Content
) : ICommand<ChatMessageDto>;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/SendTextMessageCommandValidator.cs`
* **How:**

```csharp
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
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/SendTextMessageCommandHandler.cs`
* **Why:** Gọi Entity `ChatMessage.CreateText` $\rightarrow$ Lưu DB $\rightarrow$ Gọi Entity `Conversation.UpdateLastMessage` $\rightarrow$ Đẩy tín hiệu Realtime qua `IChatNotificationService`.
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class SendTextMessageCommandHandler : ICommandHandler<SendTextMessageCommand, ChatMessageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SendTextMessageCommandHandler(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        IChatNotificationService chatNotificationService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _chatNotificationService = chatNotificationService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ChatMessageDto>> Handle(SendTextMessageCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ChatMessageDto>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure<ChatMessageDto>(new Error("Conversation.NotFound", "Không tìm thấy hội thoại."));

        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
            return Result.Failure<ChatMessageDto>(new Error("Chat.Forbidden", "Bạn không thuộc cuộc trò chuyện này."));

        // Gọi Domain Entity để tự bảo vệ Invariant
        var messageResult = ChatMessage.CreateText(conversation.Id, currentUserId, request.Content);
        if (messageResult.IsFailure)
            return Result.Failure<ChatMessageDto>(messageResult.Error);

        var message = messageResult.Value;

        // Lưu tin nhắn và cập nhật trạng thái hội thoại
        await _chatMessageRepository.AddAsync(message, ct);
        conversation.UpdateLastMessage(message.Id, message.Type, message.Content, message.CreatedAt, currentUserId);
        await _conversationRepository.UpdateAsync(conversation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Type,
            message.Content,
            message.VoiceUrl,
            message.VoiceDuration,
            message.CallLogData,
            message.IsRead,
            message.CreatedAt
        );

        // Phát realtime notification đến người nhận
        var recipientId = conversation.GetPartnerId(currentUserId);
        await _chatNotificationService.SendMessageNotificationAsync(recipientId, dto, ct);

        return Result.Success(dto);
    }
}
```

---

### 4.3 `SendVoiceMessageCommand` (Gửi Tin Nhắn Thoại Rảnh Tay 120s)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/SendVoiceMessageCommand.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record SendVoiceMessageCommand(
    Guid ConversationId,
    string VoiceUrl,
    int VoiceDuration
) : ICommand<ChatMessageDto>;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/SendVoiceMessageCommandValidator.cs`
* **How:**

```csharp
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
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/SendVoiceMessageCommandHandler.cs`
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class SendVoiceMessageCommandHandler : ICommandHandler<SendVoiceMessageCommand, ChatMessageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SendVoiceMessageCommandHandler(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        IChatNotificationService chatNotificationService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _chatNotificationService = chatNotificationService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ChatMessageDto>> Handle(SendVoiceMessageCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ChatMessageDto>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure<ChatMessageDto>(new Error("Conversation.NotFound", "Không tìm thấy hội thoại."));

        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
            return Result.Failure<ChatMessageDto>(new Error("Chat.Forbidden", "Bạn không thuộc cuộc trò chuyện này."));

        var messageResult = ChatMessage.CreateVoice(conversation.Id, currentUserId, request.VoiceUrl, request.VoiceDuration);
        if (messageResult.IsFailure)
            return Result.Failure<ChatMessageDto>(messageResult.Error);

        var message = messageResult.Value;

        await _chatMessageRepository.AddAsync(message, ct);
        conversation.UpdateLastMessage(message.Id, message.Type, message.Content, message.CreatedAt, currentUserId);
        await _conversationRepository.UpdateAsync(conversation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Type,
            message.Content,
            message.VoiceUrl,
            message.VoiceDuration,
            message.CallLogData,
            message.IsRead,
            message.CreatedAt
        );

        var recipientId = conversation.GetPartnerId(currentUserId);
        await _chatNotificationService.SendMessageNotificationAsync(recipientId, dto, ct);

        return Result.Success(dto);
    }
}
```

---

### 4.4 `MarkConversationAsReadCommand` (Đánh Dấu Đã Xem Tin Nhắn)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/MarkConversationAsReadCommand.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record MarkConversationAsReadCommand(Guid ConversationId) : ICommand;
```

#### File 2: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/MarkConversationAsReadCommandHandler.cs`
* **Why:** Thực thi invariant `BR_CHAT_04`: Khi người dùng mở đoạn chat, toàn bộ tin nhắn chưa đọc của đối phương gửi tới được chuyển `is_read = true`, reset `UnreadCount = 0`, và thông báo cho người gửi biết qua socket.
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class MarkConversationAsReadCommandHandler : ICommandHandler<MarkConversationAsReadCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MarkConversationAsReadCommandHandler(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        IChatNotificationService chatNotificationService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _chatNotificationService = chatNotificationService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(MarkConversationAsReadCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure(new Error("Conversation.NotFound", "Không tìm thấy hội thoại."));

        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
            return Result.Failure(new Error("Chat.Forbidden", "Bạn không thuộc cuộc trò chuyện này."));

        // Lấy tất cả tin nhắn đối phương gửi mà chưa đọc
        var unreadMessages = await _chatMessageRepository.GetUnreadMessagesAsync(conversation.Id, currentUserId, ct);

        foreach (var msg in unreadMessages)
        {
            msg.MarkRead();
        }

        conversation.MarkAsRead(currentUserId);

        await _chatMessageRepository.UpdateRangeAsync(unreadMessages, ct);
        await _conversationRepository.UpdateAsync(conversation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Bắn sự kiện Read Receipt đến đối phương
        var partnerId = conversation.GetPartnerId(currentUserId);
        await _chatNotificationService.SendMessageReadNotificationAsync(partnerId, conversation.Id, currentUserId, ct);

        return Result.Success();
    }
}
```

---

### 4.5 `LogCallSessionCommand` (Ghi Nhận Kết Quả Cuộc Gọi Vào Dòng Chat)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/LogCallSessionCommand.cs`
* **Why:** Thực thi quy tắc nghiệp vụ `BR_CHAT_05 (PT-03)`: Sau khi cuộc gọi thoại kết thúc (hoàn tất, nhỡ hoặc từ chối), tự động ghi nhận một tin nhắn `CallLog` vào dòng thời gian của cả 2 bên.
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record LogCallSessionCommand(
    Guid ConversationId,
    CallStatus Status,
    int DurationSeconds,
    CallDirection Direction
) : ICommand<ChatMessageDto>;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/LogCallSessionCommandValidator.cs`
* **How:**

```csharp
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
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Commands/LogCallSessionCommandHandler.cs`
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public class LogCallSessionCommandHandler : ICommandHandler<LogCallSessionCommand, ChatMessageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LogCallSessionCommandHandler(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        IChatNotificationService chatNotificationService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _chatNotificationService = chatNotificationService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ChatMessageDto>> Handle(LogCallSessionCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ChatMessageDto>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
            return Result.Failure<ChatMessageDto>(new Error("Conversation.NotFound", "Không tìm thấy hội thoại."));

        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
            return Result.Failure<ChatMessageDto>(new Error("Chat.Forbidden", "Bạn không thuộc cuộc trò chuyện này."));

        var callLogData = new CallLogData(request.Status, request.DurationSeconds, request.Direction);
        var messageResult = ChatMessage.CreateCallLog(conversation.Id, currentUserId, callLogData);
        if (messageResult.IsFailure)
            return Result.Failure<ChatMessageDto>(messageResult.Error);

        var message = messageResult.Value;

        await _chatMessageRepository.AddAsync(message, ct);
        conversation.UpdateLastMessage(message.Id, message.Type, message.Content, message.CreatedAt, currentUserId);
        await _conversationRepository.UpdateAsync(conversation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Type,
            message.Content,
            message.VoiceUrl,
            message.VoiceDuration,
            message.CallLogData,
            message.IsRead,
            message.CreatedAt
        );

        var recipientId = conversation.GetPartnerId(currentUserId);
        await _chatNotificationService.SendMessageNotificationAsync(recipientId, dto, ct);

        return Result.Success(dto);
    }
}
```

---

## 🎯 LỆNH BUILD KIỂM CHỨNG (VERIFICATION)

Do `DependencyInjection.cs` của `Tripory.Application` đã cấu hình quét assembly tự động (`RegisterServicesFromAssembly` và `AddValidatorsFromAssembly`), toàn bộ các Handler và Validator mới này sẽ tự động được nhận diện mà không cần cấu hình thêm.

Sau khi anh hoàn thành việc gõ các file trên, hãy chạy lệnh kiểm tra:

```bash
dotnet build src/Services/Tripory/Tripory.Application/Tripory.Application.csproj
```

**Tiêu chuẩn nghiệm thu:** Lệnh trả về:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Không có bất kỳ cảnh báo hoặc lỗi biên dịch nào!
