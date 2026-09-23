# PHASE_02_USER_CHAT.md – Kế Hoạch Triển Khai Giai Đoạn 2: Hệ Thống Nhắn Tin, Ghi Âm & Gọi Thoại 1-1 (USER_CHAT_01)

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 02 – Realtime Communication & User Chat System  
> **Lựa chọn kiến trúc:** Clean Architecture + DDD + CQRS + ASP.NET Core SignalR (Kế thừa tiêu chuẩn từ `tc-ems-be` và ràng buộc trong `AGENTS.md`)  
> **Nguồn đặc tả nghiệp vụ:** `../tripory/docs/traveler/USER_CHAT_01` (`user_chat.md`, `PT-01`, `PT-02`, `PT-03`)  
> **Điều chỉnh đặc biệt (Tech Lead Approval):** Tạm thời hoãn (Deferred) tính năng chia sẻ thẻ Lịch trình (`SharedItineraryCard`), tập trung hoàn thiện 100% Nhắn tin văn bản, Tin nhắn thoại rảnh tay 120s và Cuộc gọi thoại 1-1.

---

## 🎯 1. Mục Tiêu & Phạm Vi (Scope)

* **Mục tiêu:** Thiết lập nền tảng giao tiếp thời gian thực 1-1 giữa các thành viên trên Tripory, hỗ trợ trao đổi tin nhắn văn bản, tin nhắn thoại rảnh tay (Hands-free Voice Message $\le 120\text{s}$) và cuộc gọi thoại trực tiếp (Audio Call kèm WebRTC Signaling).
* **Phạm vi triển khai (In-Scope):**
  * Hộp thư hội thoại cá nhân 1-1 (`/messages` và `/messages/{id}`).
  * Danh sách hội thoại sắp xếp theo tin nhắn mới nhất, huy hiệu tin chưa đọc (Unread Count).
  * Nhắn tin văn bản thời gian thực qua SignalR Hub, đồng bộ trạng thái đã xem (`is_read`).
  * Tin nhắn thoại rảnh tay tối đa 120s, upload file âm thanh lưu trữ vào MinIO/S3 cục bộ, lưu metadata thời lượng.
  * Cuộc gọi thoại 1-1 với 5 trạng thái (`calling`, `ringing`, `incoming`, `connected`, `ended`), chuyển tiếp tín hiệu WebRTC Signaling và ghi vết `call_log` vào đoạn chat sau khi kết thúc cuộc gọi.
* **Phạm vi cắt giảm / Tạm hoãn (Deferred / Out-of-Scope):**
  * ⏸️ **Thẻ lịch trình du lịch (`SharedItineraryCard`):** Đã tách rời và ghi chú TODO, sẽ kích hoạt lại khi hoàn thành Phase Lịch trình `ITINERARY_01`.
  * ⏸️ **Nhóm chat theo chuyến đi (Trip Group Chat):** Quy hoạch phát triển ở giai đoạn cộng đồng sau này.
  * ⏸️ **Cuộc gọi Video trực tiếp (Video Call):** Chưa thực hiện trong Phase này.

---

## 📊 2. Bảng Tiến Độ Triển Khai 3 Milestones

```
[Milestone 2.1: Chat Core 1-1 & Realtime SignalR]
                       │
                       ▼
[Milestone 2.2: Hands-free Voice Message 120s & MinIO Storage]
                       │
                       ▼
[Milestone 2.3: 1-1 Audio Call & WebRTC Signaling Lifecycle]
```

| Milestone | Nội dung trọng tâm | Trạng thái | Ghi chú |
| :---: | :--- | :---: | :--- |
| **2.1** | **Chat Core (Text 1-1, Hub Realtime & Read Receipt)** | `[~] Đang triển khai` | Đã xong Domain & Application, đang làm Bước 3: Persistence |
| **2.2** | **Voice Messages 120s & MinIO Storage** | `[ ] Chờ thực hiện` | MinIO Docker, Upload Audio API, Voice metadata |
| **2.3** | **Audio Call 1-1 & WebRTC Signaling** | `[ ] Chờ thực hiện` | Signaling Hub, 5-State Machine, Call Log |

---

## 📊 Bảng Tiến Độ Tổng Quan (Execution Checklist)

| Bước | Hạng mục | Trạng thái | Ghi chú |
| :---: | :--- | :---: | :--- |
| **01** | Domain Layer (`Tripory.Domain` - Module Chat) | `[x] Hoàn thành` | `Conversation`, `ChatMessage`, Enums, `CallLogData`, Invariants |
| **02** | Application Layer (CQRS UseCases & Hub Ports) | `[x] Hoàn thành` | Commands, Queries, Handlers, Validators, DTOs, Ports |
| **03** | Persistence Layer (PostgreSQL EF Core) | `[x] Hoàn thành` | Configurations, `ApplicationDbContext`, Repositories, Migrations |
| **04** | Infrastructure Layer (SignalR & MinIO) | `[ ] ĐANG THỰC HIỆN` | `ChatHub`, `MinioAudioStorageService`, Realtime Notifications |
| **05** | API Layer (REST Endpoints & Hub Mapping) | `[ ] Chờ thực hiện` | `ChatController`, Hub route `/hubs/chat`, CORS credentials |
| **06** | Verification & End-to-End Testing | `[ ] Chờ thực hiện` | Test 2 session chat realtime, Voice playback, WebRTC Call |

---

## 🏗️ 3. Quy Trình Triển Khai "Từ Trong Ra Ngoài" (Inside-Out) Cho Phase 02

```
[x] [Bước 1: Domain Layer (Module Chat Entities & Invariants)]
              │
              ▼
[x] [Bước 2: Application Layer (UseCases, CQRS Handlers & SignalR Ports)]
              │
              ▼
[x] [Bước 3: Persistence Layer (Fluent API, DbContext & Migration Chat Tables)]
              │
              ▼
[x] [Bước 4: Infrastructure Layer (SignalR ChatHub & MinIO Audio Storage)]
              │
              ▼
[ ] [Bước 5: API Layer (ChatController, REST History, SignalR Hub Mapping)]◄── [TIẾP THEO]
              │
              ▼
[ ] [Bước 6: Verification & End-to-End Testing (2 User Realtime Session)]
```

---

### BƯỚC 1: Tầng `Tripory.Domain` (Module Chat - Pure C#)
* **Trách nhiệm:** Bảo vệ toàn bộ invariants nghiệp vụ trò chuyện 1-1, giới hạn độ dài tin nhắn và thời lượng ghi âm thoại.
* **Entities:**
  * `Conversation` (Aggregate Root):
    * `Id` (Guid), `User1Id` (Guid), `User2Id` (Guid), `LastMessageId` (Guid?), `LastMessageAt` (DateTimeOffset), `UnreadCountUser1` (int), `UnreadCountUser2` (int), `CreatedAt`, `UpdatedAt`.
    * Phương thức nghiệp vụ: `Create(user1Id, user2Id)`, `UpdateLastMessage(...)`, `MarkAsRead(userId)`, `GetPartnerId(userId)`.
    * Invariants: `User1Id != User2Id`.
  * `ChatMessage` (Entity):
    * `Id` (Guid), `ConversationId` (Guid), `SenderId` (Guid), `Type` (Enum: `Text`, `Voice`, `Itinerary`, `CallLog`), `Content` (string?), `VoiceUrl` (string?), `VoiceDuration` (int?), `CallLogData` (VO/JSON), `IsRead` (bool), `CreatedAt`.
    * Phương thức nghiệp vụ: `CreateText(...)`, `CreateVoice(...)`, `CreateCallLog(...)`, `MarkRead()`.
* **Value Objects & Enums:**
  * `MessageType`: `Text = 1`, `Voice = 2`, `Itinerary = 3`, `CallLog = 4`.
  * `CallStatus`: `Completed = 1`, `Missed = 2`, `Declined = 3`.
  * `CallDirection`: `Inbound = 1`, `Outbound = 2`.
  * `CallLogData`: Record/Value Object lưu `Status`, `Duration`, `Direction`.
* **Invariants Nghiệp vụ:**
  * `BR_CHAT_01`: Hội thoại chỉ thuộc về 2 người dùng duy nhất (1-1).
  * `BR_CHAT_02`: Tin nhắn văn bản không được rỗng, độ dài $\le 2000$ ký tự.
  * `BR_CHAT_02 (PT-02)`: Tin nhắn thoại thời lượng từ $1$ đến $120$ giây.
  * `BR_CHAT_05 (PT-03)`: Bản ghi `CallLog` bắt buộc có trạng thái và thời lượng đàm thoại.

---

### BƯỚC 2: Tầng `Tripory.Application` (UseCases, CQRS & Abstraction Ports)
* **UseCases:**
  * `GetOrCreateConversationCommand`: Tìm hoặc tạo mới hội thoại giữa 2 người dùng.
  * `GetConversationsQuery`: Lấy danh sách hội thoại của người dùng hiện tại (kèm Unread count và đối phương).
  * `GetMessagesQuery`: Lấy lịch sử tin nhắn phân trang (Paged/Cursor).
  * `SendTextMessageCommand`: Gửi tin nhắn văn bản, lưu DB và kích hoạt push realtime qua Port.
  * `SendVoiceMessageCommand`: Gửi tin nhắn thoại kèm URL và thời lượng.
  * `MarkConversationAsReadCommand`: Đánh dấu đã xem toàn bộ tin nhắn trong hội thoại.
  * `LogCallSessionCommand`: Ghi nhận kết quả cuộc gọi thoại vào dòng chat.
* **Abstraction Ports:**
  * `IChatNotificationService`: Port để Application gửi tín hiệu realtime xuống Client mà không phụ thuộc trực tiếp vào package SignalR.
  * `IConversationRepository` & `IChatMessageRepository`: Thao tác dữ liệu hội thoại và tin nhắn.
  * `IAudioStorageService`: Port lưu trữ file âm thanh (MinIO/S3).

---

### BƯỚC 3: Tầng `Tripory.Persistence` (PostgreSQL EF Core)
* **Cấu trúc:**
  * `ApplicationDbContext`: Bổ sung `DbSet<Conversation>`, `DbSet<ChatMessage>`.
  * `Configurations/`:
    * `ConversationConfiguration.cs`: Schema `identity`, Unique Index trên `(user1_id, user2_id)` để ngăn chặn trùng lặp hội thoại.
    * `ChatMessageConfiguration.cs`: Composite Index trên `(conversation_id, created_at DESC)` để tối ưu hóa truy vấn tải tin nhắn gần nhất và phân trang nhanh.
* **Migration:** Chạy `dotnet ef migrations add Add_Chat_Module_Tables` và áp vào Postgres.

---

### BƯỚC 4: Tầng `Tripory.Infrastructure` (SignalR & MinIO Storage)
* **Cấu hình Docker:** Bổ sung container `minio` (S3 Compatible) vào `docker-compose.yml`.
* **SignalR Hub (`ChatHub.cs`):**
  * Xác thực qua JWT Token truyền trong query string (`/hubs/chat?access_token=...`).
  * Quản lý kết nối theo `Context.UserIdentifier` (`UserId`).
  * Phương thức gửi nhận tin nhắn realtime: `SendMessage`, `ReceiveMessage`, `MessageRead`.
  * Phương thức WebRTC Signaling (PT-03): `StartCall`, `AcceptCall`, `RejectCall`, `EndCall`, `SendIceCandidate`.
* **Triển khai Storage:** `MinioAudioStorageService` hiện thực hóa `IAudioStorageService`.

---

### BƯỚC 5: Tầng `Tripory.API` (Endpoints & Composition Root)
* **REST Endpoints (`ChatController.cs`):**
  * `GET /api/v1/chat/conversations`: Danh sách cuộc trò chuyện của tôi.
  * `GET /api/v1/chat/conversations/{id}/messages`: Phân trang lịch sử tin nhắn.
  * `POST /api/v1/chat/conversations/{partnerId}`: Khởi tạo/Lấy hội thoại với bạn bè.
  * `POST /api/v1/chat/voice`: Endpoint tiếp nhận file âm thanh thu từ mic (`multipart/form-data`) $\rightarrow$ trả về `voice_url`.
* **SignalR Endpoint Mapping:**
  * `app.MapHub<ChatHub>("/hubs/chat")`.
  * Cấu hình CORS cho phép `AllowCredentials()` để WebSocket hoạt động mượt mà.

---

### BƯỚC 6: Tiêu Chuẩn Nghiệm Thu & Kiểm Chứng (Quality Gate)
1. `dotnet build` toàn bộ solution đạt `0 Errors, 0 Warnings`.
2. Mở 2 tab trình duyệt ẩn danh (2 user khác nhau):
   * User A gửi tin nhắn văn bản $\rightarrow$ User B nhận được tức thì qua SignalR mà không cần reload trang.
   * User B mở chat $\rightarrow$ Trạng thái `is_read` tự động cập nhật, Unread badge của User B về 0.
   * User A upload file âm thanh $\rightarrow$ Lưu thành công vào MinIO, User B bấm nghe được audio.
   * User A bấm nút Gọi điện $\rightarrow$ User B hiện modal Cuộc gọi đến kèm chuông đổ lặp (`loop = true`), khi gác máy tự động sinh `CallLog` vào lịch sử chat.
3. Không vi phạm bất kỳ điều cấm nào trong [AGENTS.md](../AGENTS.md).
