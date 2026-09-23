# HƯỚNG DẪN TRIỂN KHAI BƯỚC 4: INFRASTRUCTURE LAYER (`Tripory.Infrastructure`)
### Kèm Phân Tích Kiến Trúc 5W1H, SignalR Unified ChatHub, WebRTC Signaling & Dịch Vụ Lưu Trữ Âm Thanh Voice Message

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 02 – Realtime Communication & User Chat System (`USER_CHAT_01`)  
> **Tiêu chuẩn tuân thủ:** Clean Architecture + DIP + `AGENTS.md` (Anti-Vibe Coding, Hướng dẫn Micro-step 5W1H)  
> **Nguồn đặc tả nghiệp vụ:** `../tripory/docs/traveler/USER_CHAT_01/` (`user_chat.md`, `PT-01`, `PT-02`, `PT-03`)  
> **Quyết định kiến trúc đã chốt:**
> * **Vấn đề 1:** Chọn **Phương án 1 (Unified Hub `ChatHub`)** tích hợp cả Nhắn tin và WebRTC Audio Call Signaling tại route `/hubs/chat` để tiết kiệm tài nguyên kết nối socket.
> * **Vấn đề 2:** Triển khai **Port `IAudioStorageService`** tại tầng Application và Adapter **`LocalAudioStorageService`** tại Infrastructure (0MB RAM overhead, không cần container MinIO phức tạp trong giai đoạn dev).

---

## 📌 MỤC LỤC TRÌNH TỰ THỰC HIỆN BƯỚC 4

1. [Phân Tích Kiến Trúc 5W1H Cho Tầng Infrastructure](#-phân-tích-kiến-trúc-5w1h-cho-tầng-infrastructure)
2. [Khối 1: Khai Báo Port `IAudioStorageService` (Tầng Application)](#-khối-1-khai-báo-port-iaudiostorageservice-tầng-application)
3. [Khối 2: Hiện Thực Hóa `LocalAudioStorageService` (Tầng Infrastructure)](#-khối-2-hiện-thực-hóa-localaudiostorageservice-tầng-infrastructure)
4. [Khối 3: Xây Dựng SignalR Unified `ChatHub.cs`](#-khối-3-xây-dựng-signalr-unified-chathubcs)
   * 3.1 Xác thực JWT qua WebSocket
   * 3.2 Quản lý phòng chat hội thoại (`JoinConversation`, `LeaveConversation`)
   * 3.3 Chuyển tiếp tín hiệu WebRTC Audio Call (`CallUser`, `AcceptCall`, `RejectCall`, `EndCall`, `SendIceCandidate`)
5. [Khối 4: Hoàn Thiện Bộ Điều Phối Realtime `ChatNotificationService.cs`](#-khối-4-hoàn-thiện-bộ-điều-phối-realtime-chatnotificationservicecs)
6. [Khối 5: Đăng Ký Dependency Injection Trong `Tripory.Infrastructure`](#-khối-5-đăng-ký-dependency-injection-trong-triporyinfrastructure)
7. [Khối 6: Cấu Hình SignalR, CORS & Static Files Tại `Tripory.API/Program.cs`](#-khối-6-cấu-hình-signalr-cors--static-files-tại-triporyapiprogramcs)
8. [Khối 7: Tiêu Chuẩn Kiểm Chứng & Nghiệm Thu (Quality Gate)](#-khối-7-tiêu-chuẩn-kiểm-chứng--nghiệm-thu-quality-gate)
9. [📚 Phụ Lục: Giải Đáp Chuyên Sâu Về Chi Phí & Cơ Chế WebSocket Handshake](#-phụ-lục-giải-đáp-chuyên-sâu-về-chi-phí--cơ-chế-websocket-handshake)

---

## 🏛️ PHÂN TÍCH KIẾN TRÚC 5W1H CHO TẦNG INFRASTRUCTURE

* **What (Tầng Infrastructure làm gì trong Bước 4):**
  * Đóng vai trò là tầng hiện thực hóa các dịch vụ I/O, mạng và thiết bị ngoại vi mà Domain/Application không được phép phụ thuộc trực tiếp.
  * Cụ thể: Mở kết nối thời gian thực hai chiều WebSocket bằng **ASP.NET Core SignalR**, chuyển tiếp tín hiệu cuộc gọi thoại WebRTC Signaling, và lưu trữ dữ liệu tập tin âm thanh từ microphone vào đĩa cứng máy chủ.
* **Why (Tại sao phải tách Port & Adapter theo nguyên tắc DIP):**
  * Tầng Application chỉ cần biết "hãy lưu file âm thanh này và cho tôi URL" (`IAudioStorageService`), hoặc "hãy phát tin nhắn này đến người nhận" (`IChatNotificationService`).
  * Tầng Application **hoàn toàn không được biết** tin nhắn được đẩy qua SignalR, Firebase, hay RabbitMQ; cũng không được biết file được lưu trên ổ cứng cục bộ hay AWS S3. Nhờ đó, ta có thể đổi công nghệ lưu trữ và realtime mà không cần đụng đến logic nghiệp vụ.
* **Where (Vị trí các thành phần):**
  * Port trừu tượng: `src/Services/Tripory/Tripory.Application/Abstractions/Storage/`
  * Hub SignalR: `src/Services/Tripory/Tripory.Infrastructure/Hubs/`
  * Dịch vụ Storage: `src/Services/Tripory/Tripory.Infrastructure/Implementations/Storage/`
  * Dịch vụ Realtime: `src/Services/Tripory/Tripory.Infrastructure/Implementations/Realtime/`
* **When (Khi nào kích hoạt các thành phần này):**
  * Khi client thiết lập kết nối WebSocket tới server: `/hubs/chat?access_token=...`
  * Khi người dùng upload file âm thanh qua `POST /api/v1/chat/voice-upload`.
  * Khi một Handler nghiệp vụ gửi tin nhắn thành công và gọi `_chatNotificationService.SendMessageNotificationAsync(...)`.
* **Who (Ai quản lý phiên kết nối):**
  * SignalR Hub quản lý phiên làm việc theo `Context.UserIdentifier` (tương ứng với claim `ClaimTypes.NameIdentifier` - tức `UserId` của người dùng đã xác thực JWT).
* **How (Cách thức triển khai):** Thực hiện tuần tự theo 6 khối bên dưới.

---

## 🔌 KHỐI 1: KHAI BÁO PORT `IAudioStorageService` (TẦNG APPLICATION)

* **Why (Tại sao):** Tuân thủ **Pattern 4 trong `AGENTS.md`** (Tách Interface qua biên DIP). Tầng Application sở hữu giao diện trừu tượng để phục vụ use case gửi tin nhắn thoại (`SendVoiceMessageCommand`).
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Abstractions/Storage/IAudioStorageService.cs`
* **How (Mã nguồn):**

```csharp
namespace Tripory.Application.Abstractions.Storage;

public interface IAudioStorageService
{
    /// <summary>
    /// Lưu trữ file âm thanh và trả về URL công khai để truy cập.
    /// </summary>
    /// <param name="fileStream">Dòng dữ liệu file âm thanh (Stream)</param>
    /// <param name="fileName">Tên file gốc</param>
    /// <param name="contentType">Định dạng file (audio/webm, audio/wav, audio/mpeg...)</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Đường dẫn URL công khai (Public URL)</returns>
    Task<string> SaveAudioAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Xóa file âm thanh theo đường dẫn URL (khi cần dọn dẹp)
    /// </summary>
    Task DeleteAudioAsync(string fileUrl, CancellationToken ct = default);
}
```

---

## 💾 KHỐI 2: HIỆN THỰC HÓA `LocalAudioStorageService` (TẦNG INFRASTRUCTURE)

* **Why (Tại sao):** Lưu file ghi âm rảnh tay 120s (PT-02 §6) vào thư mục tĩnh `uploads/voices` trên server, sinh tên file duy nhất theo định dạng `voice_{timestamp}_{uuid}{ext}` và trả về URL đầy đủ có Scheme & Host để trình duyệt client phát âm thanh mượt mà.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Infrastructure/Implementations/Storage/LocalAudioStorageService.cs`
* **How (Mã nguồn):**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tripory.Application.Abstractions.Storage;

namespace Tripory.Infrastructure.Implementations.Storage;

public class LocalAudioStorageService : IAudioStorageService
{
    private const string UploadFolder = "uploads/voices";
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LocalAudioStorageService> _logger;

    public LocalAudioStorageService(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LocalAudioStorageService> logger)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<string> SaveAudioAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var rootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        var targetDirectory = Path.Combine(rootPath, UploadFolder);
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = contentType switch
            {
                "audio/webm" => ".webm",
                "audio/wav" or "audio/x-wav" => ".wav",
                "audio/ogg" => ".ogg",
                "audio/mp3" or "audio/mpeg" => ".mp3",
                _ => ".webm"
            };
        }

        // Quy chuẩn tên file: voice_{timestamp}_{uuid}{ext} theo PT-02 §6
        var uniqueFileName = $"voice_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetDirectory, uniqueFileName);

        await using (var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await fileStream.CopyToAsync(outputStream, ct);
        }

        _logger.LogInformation("Đã lưu file âm thanh tại: {Path}", fullPath);

        // Sinh URL đầy đủ bao gồm Scheme & Host nếu có HttpContext (ví dụ: http://localhost:5251/uploads/voices/voice_xxx.webm)
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null)
        {
            return $"{request.Scheme}://{request.Host}/{UploadFolder}/{uniqueFileName}";
        }

        return $"/{UploadFolder}/{uniqueFileName}";
    }

    public Task DeleteAudioAsync(string fileUrl, CancellationToken ct = default)
    {
        try
        {
            var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
            var relativePath = uri.IsAbsoluteUri ? uri.AbsolutePath.TrimStart('/') : fileUrl.TrimStart('/');

            var rootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;

            var fullPath = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Đã xóa file âm thanh tại: {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể xóa file âm thanh tại URL: {Url}", fileUrl);
        }

        return Task.CompletedTask;
    }
}
```

---

## ⚡ KHỐI 3: XÂY DỰNG SIGNALR UNIFIED `ChatHub.cs`

* **Why (Tại sao):**
  * Hiện thực **Phương án 1 (Unified Hub)**: Gom toàn bộ kết nối Chat 1-1, Room management và WebRTC Call Signaling vào chung 1 Hub tại `/hubs/chat` để client chỉ cần mở đúng **1 kết nối WebSocket duy nhất**.
  * Bắt buộc có `[Authorize]` để bảo đảm mọi socket kết nối đều được định danh qua JWT Token.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Infrastructure/Hubs/ChatHub.cs`
* **How (Mã nguồn):**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Tripory.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Người dùng {UserId} đã kết nối WebSocket qua ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        // Tham gia nhóm cá nhân theo UserId để dễ dàng gửi notification đích danh
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Người dùng {UserId} đã ngắt kết nối WebSocket: {ConnectionId}", userId, Context.ConnectionId);

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    #region 1. Room / Conversation Groups

    /// <summary>
    /// Tham gia phòng chat của hội thoại cụ thể (khi người dùng mở cửa sổ chat đó)
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        _logger.LogDebug("User {UserId} tham gia phòng chat hội thoại: {ConversationId}", GetCurrentUserId(), conversationId);
    }

    /// <summary>
    /// Rời phòng chat của hội thoại (khi người dùng đóng modal chat hoặc chuyển sang cuộc trò chuyện khác)
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        _logger.LogDebug("User {UserId} rời phòng chat hội thoại: {ConversationId}", GetCurrentUserId(), conversationId);
    }

    #endregion

    #region 2. WebRTC Audio Call Signaling (PT-03 §6)

    /// <summary>
    /// Bắt đầu gọi cho một người dùng khác (chuyển tiếp SDP Offer)
    /// </summary>
    public async Task CallUser(string targetUserId, object offer)
    {
        var callerId = GetCurrentUserId();
        _logger.LogInformation("Cuộc gọi đi từ {CallerId} tới {TargetUserId}", callerId, targetUserId);

        await Clients.User(targetUserId).SendAsync("IncomingCall", new
        {
            CallerId = callerId,
            Offer = offer,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Chấp nhận cuộc gọi (chuyển tiếp SDP Answer từ người nhận về người gọi)
    /// </summary>
    public async Task AcceptCall(string callerUserId, object answer)
    {
        var responderId = GetCurrentUserId();
        _logger.LogInformation("Cuộc gọi được chấp nhận bởi {ResponderId} từ người gọi {CallerUserId}", responderId, callerUserId);

        await Clients.User(callerUserId).SendAsync("CallAccepted", new
        {
            ResponderId = responderId,
            Answer = answer,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Từ chối cuộc gọi
    /// </summary>
    public async Task RejectCall(string callerUserId, string reason)
    {
        var responderId = GetCurrentUserId();
        _logger.LogInformation("Cuộc gọi bị từ chối bởi {ResponderId}. Lý do: {Reason}", responderId, reason);

        await Clients.User(callerUserId).SendAsync("CallRejected", new
        {
            ResponderId = responderId,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Người dùng bận" : reason
        });
    }

    /// <summary>
    /// Kết thúc cuộc gọi đàm thoại
    /// </summary>
    public async Task EndCall(string partnerUserId)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Cuộc gọi kết thúc bởi {UserId}", userId);

        await Clients.User(partnerUserId).SendAsync("CallEnded", new
        {
            EndedBy = userId
        });
    }

    /// <summary>
    /// Chuyển tiếp ICE Candidate để thiết lập kết nối âm thanh P2P trực tiếp
    /// </summary>
    public async Task SendIceCandidate(string targetUserId, object candidate)
    {
        var senderId = GetCurrentUserId();
        await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", new
        {
            SenderId = senderId,
            Candidate = candidate
        });
    }

    #endregion

    private string? GetCurrentUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.UserIdentifier;
    }
}
```

---

## 📢 KHỐI 4: HOÀN THIỆN BỘ ĐIỀU PHỐI REALTIME `ChatNotificationService.cs`

* **Why (Tại sao):** Thay thế đoạn code log tạm thời (stub) trước đây bằng việc inject `IHubContext<ChatHub>`. Khi các Command Handlers (gửi tin nhắn, đánh dấu đã đọc) hoàn tất ghi vào DB, service này lập tức phát sự kiện xuống WebSocket cho phía người nhận.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Infrastructure/Implementations/Realtime/ChatNotificationService.cs`
* **How (Mã nguồn):**

```csharp
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Infrastructure.Hubs;

namespace Tripory.Infrastructure.Implementations.Realtime;

public class ChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatNotificationService> _logger;

    public ChatNotificationService(
        IHubContext<ChatHub> hubContext,
        ILogger<ChatNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendMessageNotificationAsync(Guid recipientId, ChatMessageDto message, CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime tin nhắn mới đến User {RecipientId} trong hội thoại {ConversationId}", recipientId, message.ConversationId);

        // 1. Gửi trực tiếp đến User cụ thể (bất kể đang ở tab nào)
        await _hubContext.Clients.User(recipientId.ToString())
            .SendAsync("ReceiveMessage", message, cancellationToken: ct);

        // 2. Gửi đồng thời vào nhóm phòng chat của hội thoại (cập nhật màn hình chat đang mở)
        await _hubContext.Clients.Group($"conv_{message.ConversationId}")
            .SendAsync("ReceiveConversationMessage", message, cancellationToken: ct);
    }

    public async Task SendMessageReadNotificationAsync(
        Guid senderId,
        Guid conversationId,
        Guid readerId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime thông báo đã xem trong hội thoại {ConversationId} đến User {SenderId}", conversationId, senderId);

        var payload = new
        {
            ConversationId = conversationId,
            ReaderId = readerId,
            ReadAt = DateTimeOffset.UtcNow
        };

        // Gửi thông báo đến người gửi ban đầu để hiển thị tick xanh
        await _hubContext.Clients.User(senderId.ToString())
            .SendAsync("MessageRead", payload, cancellationToken: ct);

        // Gửi vào room hội thoại
        await _hubContext.Clients.Group($"conv_{conversationId}")
            .SendAsync("ConversationRead", payload, cancellationToken: ct);
    }

    public async Task SendConversationUpdatedNotificationAsync(
        Guid recipientId,
        ConversationDto conversation,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Phát realtime cập nhật danh sách hội thoại đến User {RecipientId}", recipientId);

        await _hubContext.Clients.User(recipientId.ToString())
            .SendAsync("ConversationUpdated", conversation, cancellationToken: ct);
    }
}
```

---

## 🛠️ KHỐI 5: ĐĂNG KÝ DEPENDENCY INJECTION TRONG `Tripory.Infrastructure`

* **Why (Tại sao):** Đăng ký dịch vụ `AddSignalR()`, bọc `IAudioStorageService` với `LocalAudioStorageService`, và gắn `ChatNotificationService`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Infrastructure/DependencyInjection.cs`
* **How (Mã nguồn):**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.Abstractions.Storage;
using Tripory.Infrastructure.Configurations;
using Tripory.Infrastructure.Implementations.Realtime;
using Tripory.Infrastructure.Implementations.Security;
using Tripory.Infrastructure.Implementations.Storage;

namespace Tripory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Bind JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // 2. HttpContextAccessor
        services.AddHttpContextAccessor();

        // 3. Security Services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 4. SignalR & Realtime Communication
        services.AddSignalR();
        services.AddScoped<IChatNotificationService, ChatNotificationService>();

        // 5. Audio / File Storage Service
        services.AddScoped<IAudioStorageService, LocalAudioStorageService>();

        return services;
    }
}
```

---

## 🌐 KHỐI 6: CẤU HÌNH SIGNALR, CORS & STATIC FILES TẠI `Tripory.API/Program.cs`

* **Why (Tại sao):**
  1. **WebSocket JWT Handshake:** Trình duyệt **không thể gửi custom header `Authorization`** khi mở WebSocket, nên SignalR client sẽ truyền qua Query String: `/hubs/chat?access_token=...`. Cần thêm sự kiện `JwtBearerEvents.OnMessageReceived` để đọc token này.
  2. **CORS SignalR Requirement:** SignalR bắt buộc phải có `.AllowCredentials()`. Tuy nhiên, quy chuẩn bảo mật CORS của trình duyệt **cấm dùng chung `AllowAnyOrigin()` với `AllowCredentials()`**. Ta phải đổi thành `.SetIsOriginAllowed(_ => true).AllowCredentials()`.
  3. **Static File Serving:** Thêm `app.UseStaticFiles()` để trình duyệt có thể truy cập và phát âm thanh từ link `uploads/voices/...`.
  4. **Hub Route Mapping:** Thêm `app.MapHub<ChatHub>("/hubs/chat")`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.API/Program.cs`
* **How (Mã nguồn hoàn chỉnh):**

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Tripory.API.Middlewares;
using Tripory.Application;
using Tripory.Infrastructure;
using Tripory.Infrastructure.Configurations;
using Tripory.Infrastructure.Hubs;
using Tripory.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các tầng kiến trúc
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Cấu hình Controllers & Global Exception Handler
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Cấu hình JWT Authentication (kèm hỗ trợ WebSockets cho SignalR)
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Chưa cấu hình JwtOptions trong appsettings.json.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // ✅ Bắt access_token từ Query String khi kết nối WebSocket SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 4. Cấu hình Swagger kèm nút Authorize JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tripory API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập Token theo cú pháp: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. Cấu hình CORS hỗ trợ SignalR AllowCredentials
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Bắt buộc cho WebSocket SignalR
    });
});

var app = builder.Build();

// Pipeline Middleware
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tripory API v1"));
}

// Cho phép truy cập file tĩnh (nghe audio từ thư mục uploads/voices)
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ✅ Ánh xạ SignalR Hub
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
```

---

## 🛡️ KHỐI 7: TIÊU CHUẨN KIỂM CHỨNG & NGHIỆM THU (QUALITY GATE)

Sau khi anh hoàn thành việc viết và lưu các file trên, mở Terminal và chạy lệnh kiểm chứng toàn bộ solution:

```bash
dotnet build Tripory.slnx
```

**Tiêu chuẩn nghiệm thu thành công:**
```text
  BuildingBlocks.Core -> ...\BuildingBlocks.Core.dll
  Tripory.Domain -> ...\Tripory.Domain.dll
  Tripory.Application -> ...\Tripory.Application.dll
  Tripory.Infrastructure -> ...\Tripory.Infrastructure.dll
  Tripory.Persistence -> ...\Tripory.Persistence.dll
  Tripory.API -> ...\Tripory.API.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## 📚 PHỤ LỤC: GIẢI ĐÁP CHUYÊN SÂU VỀ CHI PHÍ & CƠ CHẾ WEBSOCKET HANDSHAKE

### Câu hỏi 1: Tại sao WebSocket của trình duyệt không gửi được Header `Authorization`?
* **Bản chất kỹ thuật:** Chuẩn W3C WebSocket API trong trình duyệt (`new WebSocket(url)`) **cố tình không cho phép lập trình viên thêm custom HTTP headers** vì lý do an ninh (chống tấn công CSRF qua WebSocket Handshake).
* **Giải pháp chuẩn ngành của SignalR:** Client gửi Token qua tham số Query String `?access_token=<JWT_TOKEN>`. Khi bắt đầu kết nối, server ASP.NET Core kiểm tra đường dẫn bắt đầu bằng `/hubs` và lấy token từ query gán vào `context.Token` trước khi trình xác thực JwtBearer kiểm tra chữ ký.

### Câu hỏi 2: Chi phí thực tế khi chuyển từ `LocalAudioStorageService` sang `MinIO / S3` sau này là gì?
1. **Chi phí RAM:** Container MinIO tiêu tốn thường trực từ `400MB – 800MB RAM` do cơ chế in-memory metadata locking.
2. **Chi phí khởi tạo:** Phải viết thêm container phụ `minio/mc` để chạy script tự động tạo bucket `tripory-voices` và gán quyền `Public Read` (nếu không sẽ bị lỗi `403 Forbidden`).
3. **Chi phí thư viện:** Phải cài `AWSSDK.S3` hoặc `Minio` SDK và quản lý các cặp keys (`AccessKey`, `SecretKey`, `ServiceUrl`).
4. **Lợi ích khi tuân thủ DIP:** Nhờ có interface `IAudioStorageService`, việc chuyển đổi sau này chỉ là viết một class mới `S3AudioStorageService` và thay đổi đúng 1 dòng khai báo DI, toàn bộ phần còn lại của hệ thống không bị ảnh hưởng.

---

Anh hãy mở file này và tiến hành tạo từng file theo các bước trên nhé! Sau khi hoàn thành và build thành công, chúng ta sẽ sẵn sàng bước tiếp sang Bước 5.
