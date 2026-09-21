# HƯỚNG DẪN XỬ LÝ LỖI DI & TRIỂN KHAI BƯỚC 3: PERSISTENCE LAYER (`Tripory.Persistence`)
### Kèm Phân Tích Kiến Trúc 5W1H & Đăng Ký Cổng Dữ Liệu Cho Module Chat

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 02 – Realtime Communication & User Chat System  
> **Lỗi ghi nhận:** `System.AggregateException: Some services are not able to be constructed ... Unable to resolve service for type 'IConversationRepository'`  
> **Nguyên nhân kiến trúc:** Anh vừa hoàn thành xuất sắc **Bước 2 (Application Layer)**. Khi ứng dụng khởi động ở môi trường `Development`, ASP.NET Core tự động quét và kiểm tra tính toàn vẹn của Dependency Injection (`ValidateOnBuild = true`). Do các Handlers mới (`GetConversationsQueryHandler`, `SendTextMessageCommandHandler`...) đòi hỏi `IConversationRepository`, `IChatMessageRepository` và `IChatNotificationService` mà chúng ta **chưa triển khai Bước 3 (Persistence)** và **Bước 4 (Infrastructure)** nên Web Host báo lỗi thiếu đăng ký dịch vụ.

---

## 📌 MỤC LỤC TRÌNH TỰ THỰC HIỆN BƯỚC 3

1. [Khối 1: Cấu Hình Fluent API Trong Persistence](#-khối-1-cấu-hình-fluent-api-trong-persistence)
   * 1.1 `ConversationConfiguration.cs`
   * 1.2 `ChatMessageConfiguration.cs`
2. [Khối 2: Cập Nhật `ApplicationDbContext.cs`](#-khối-2-cập-nhật-applicationdbcontextcs)
3. [Khối 3: Hiện Thực Hóa 2 Repositories Cho Chat](#-khối-3-hiện-thực-hóa-2-repositories-cho-chat)
   * 3.1 `ConversationRepository.cs`
   * 3.2 `ChatMessageRepository.cs`
4. [Khối 4: Đăng Ký DI Trong Persistence](#-khối-4-đăng-ký-di-trong-persistence)
5. [Khối 5: Cung Cấp Tạm Stub `ChatNotificationService` Cho Infrastructure](#-khối-5-cung-cấp-tạm-stub-chatnotificationservice-cho-infrastructure)
6. [Khối 6: Tạo EF Core Migration & Áp Vào Postgres](#-khối-6-tạo-ef-core-migration--áp-vào-postgres)

---

## 🏛️ KHỐI 1: CẤU HÌNH FLUENT API TRONG PERSISTENCE

### 1.1 `ConversationConfiguration.cs`
* **Why (Tại sao):** Tuân thủ **Pattern 5** (`AGENTS.md`): Không dùng Data Annotations trên Domain Entity. Cần tạo schema `identity`, cấu hình quan hệ 1-N với `ChatMessage`, và tạo **Unique Index trên `(user1_id, user2_id)`** để bảo đảm ở mức DB không bao giờ bị tạo trùng lặp hội thoại giữa 2 người dùng.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Persistence/Configurations/ConversationConfiguration.cs`
* **How (Mã nguồn):**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", "identity");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.User1Id)
            .IsRequired();

        builder.Property(c => c.User2Id)
            .IsRequired();

        // Unique Index: Đảm bảo chỉ tồn tại duy nhất 1 cuộc hội thoại giữa 2 user
        builder.HasIndex(c => new { c.User1Id, c.User2Id })
            .IsUnique();

        builder.Property(c => c.LastMessageContent)
            .HasMaxLength(2000);

        builder.Property(c => c.LastMessageType)
            .HasConversion<int>();

        builder.Property(c => c.LastMessageAt);

        builder.Property(c => c.UnreadCountUser1)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.UnreadCountUser2)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Quan hệ 1-N với ChatMessage
        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

### 1.2 `ChatMessageConfiguration.cs`
* **Why (Tại sao):** Cần cấu hình lưu trữ tin nhắn văn bản, tin nhắn thoại và bản ghi cuộc gọi. Đặc biệt, cần tạo **Composite Index trên `(conversation_id, created_at DESC)`** để các truy vấn tải lịch sử chat phân trang chạy siêu tốc trên Postgres. Value Object `CallLogData` được ánh xạ qua `OwnsOne`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Persistence/Configurations/ChatMessageConfiguration.cs`
* **How (Mã nguồn):**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages", "identity");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.SenderId)
            .IsRequired();

        builder.Property(m => m.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(ChatMessage.MaxTextLength);

        builder.Property(m => m.VoiceUrl)
            .HasMaxLength(1000);

        builder.Property(m => m.VoiceDuration);

        builder.Property(m => m.IsRead)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        // Ánh xạ Value Object CallLogData
        builder.OwnsOne(m => m.CallLogData, callLogBuilder =>
        {
            callLogBuilder.Property(c => c.Status)
                .HasColumnName("call_status")
                .HasConversion<int>();

            callLogBuilder.Property(c => c.DurationSeconds)
                .HasColumnName("call_duration_seconds");

            callLogBuilder.Property(c => c.Direction)
                .HasColumnName("call_direction")
                .HasConversion<int>();
        });

        // Composite Index tối ưu truy vấn lịch sử tin nhắn
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
    }
}
```

---

## 🗄️ KHỐI 2: CẬP NHẬT `ApplicationDbContext.cs`

* **Why:** Đăng ký 2 DbSet mới để EF Core quản lý thực thể và tạo Migration.
* **Where:** `src/Services/Tripory/Tripory.Persistence/ApplicationDbContext.cs`
* **How (Bổ sung 2 dòng DbSet vào `ApplicationDbContext`):**

```csharp
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
```

---

## 🚀 KHỐI 3: HIỆN THỰC HÓA 2 REPOSITORIES CHO CHAT

### 3.1 `ConversationRepository.cs`
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Persistence/Repositories/ConversationRepository.cs`
* **How (Mã nguồn):**

```csharp
using Microsoft.EntityFrameworkCore;
using Tripory.Application.Abstractions.Data;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _context;

    public ConversationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Conversation?> GetByUsersAsync(Guid userA, Guid userB, CancellationToken ct = default)
    {
        // Chuẩn hóa theo cùng quy tắc u1 < u2
        var (u1, u2) = userA.CompareTo(userB) < 0 ? (userA, userB) : (userB, userA);

        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == u1 && c.User2Id == u2, ct);
    }

    public async Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken ct = default)
    {
        await _context.Conversations.AddAsync(conversation, ct);
    }

    public Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _context.Conversations.Update(conversation);
        return Task.CompletedTask;
    }
}
```

---

### 3.2 `ChatMessageRepository.cs`
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Persistence/Repositories/ChatMessageRepository.cs`
* **How (Mã nguồn):**

```csharp
using Microsoft.EntityFrameworkCore;
using Tripory.Application.Abstractions.Data;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly ApplicationDbContext _context;

    public ChatMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesByConversationAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.CreatedAt) // Đảo lại theo thứ tự thời gian tăng dần để client hiển thị
            .ToListAsync(ct);
    }

    public async Task AddAsync(ChatMessage message, CancellationToken ct = default)
    {
        await _context.ChatMessages.AddAsync(message, ct);
    }

    public Task UpdateAsync(ChatMessage message, CancellationToken ct = default)
    {
        _context.ChatMessages.Update(message);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        _context.ChatMessages.UpdateRange(messages);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetUnreadMessagesAsync(
        Guid conversationId,
        Guid receiverId,
        CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.SenderId != receiverId && !m.IsRead)
            .ToListAsync(ct);
    }
}
```

---

## 🔌 KHỐI 4: ĐĂNG KÝ DI TRONG PERSISTENCE

* **Where:** `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs`
* **How:** Thêm 2 dòng đăng ký Repository vào method `AddPersistence`:

```csharp
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
```

---

## ⚡ KHỐI 5: CUNG CẤP TẠM STUB `ChatNotificationService` CHO INFRASTRUCTURE

* **Why (Tại sao):** Các Handler gửi tin nhắn (`SendTextMessageCommandHandler`...) còn đòi hỏi `IChatNotificationService`. Trước khi chúng ta dựng xong SignalR Hub hoàn chỉnh ở Bước 4, ta cung cấp một class thực thi ghi log để thỏa mãn DI Container, giúp ứng dụng khởi động thành công ngay lập tức!
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Infrastructure/Implementations/Realtime/ChatNotificationService.cs`
* **How (Mã nguồn):**

```csharp
using Microsoft.Extensions.Logging;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Infrastructure.Implementations.Realtime;

public class ChatNotificationService : IChatNotificationService
{
    private readonly ILogger<ChatNotificationService> _logger;

    public ChatNotificationService(ILogger<ChatNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendMessageNotificationAsync(Guid recipientId, ChatMessageDto message, CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime tin nhắn mới đến User {RecipientId}: {Content}", recipientId, message.Content);
        return Task.CompletedTask;
    }

    public Task SendMessageReadNotificationAsync(Guid senderId, Guid conversationId, Guid readerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime thông báo đã xem trong hội thoại {ConversationId} đến User {SenderId}", conversationId, senderId);
        return Task.CompletedTask;
    }

    public Task SendConversationUpdatedNotificationAsync(Guid recipientId, ConversationDto conversation, CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime cập nhật hội thoại đến User {RecipientId}", recipientId);
        return Task.CompletedTask;
    }
}
```

*Và đăng ký vào `src/Services/Tripory/Tripory.Infrastructure/DependencyInjection.cs`:*
```csharp
using Tripory.Application.Abstractions.Realtime;
using Tripory.Infrastructure.Implementations.Realtime;

// Thêm dòng sau vào method AddInfrastructure:
services.AddScoped<IChatNotificationService, ChatNotificationService>();
```

---

## 📦 KHỐI 6: TẠO EF CORE MIGRATION & ÁP VÀO POSTGRES

Sau khi gõ xong các file trên, anh chạy 2 lệnh sau tại thư mục gốc `tripory_be`:

```bash
# 1. Tạo migration mới cho 2 bảng chat
dotnet ef migrations add Add_Chat_Module_Tables \
  --project src/Services/Tripory/Tripory.Persistence \
  --startup-project src/Services/Tripory/Tripory.API

# 2. Áp migration vào PostgreSQL
dotnet ef database update \
  --project src/Services/Tripory/Tripory.Persistence \
  --startup-project src/Services/Tripory/Tripory.API
```

Sau khi hoàn tất, anh chạy `dotnet run --project src/Services/Tripory/Tripory.API`, lỗi **AggregateException** sẽ biến mất hoàn toàn và Web API sẽ khởi động mượt mà!
