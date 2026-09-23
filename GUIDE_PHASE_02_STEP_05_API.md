# HƯỚNG DẪN THIẾT KẾ BƯỚC 5: TẦNG API – MODULE CHAT (`Tripory.API`)
### Quy Trình Hướng Dẫn Từng Bước (No-Code Blueprint) Kèm 5W1H & Đề Xuất Kỹ Thuật

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 02 – Realtime Communication & User Chat System  
> **Bước thực hiện:** Bước 05 – API Layer (REST Endpoints, Multipart Audio Upload & Swagger Docs)  
> **Tiêu chuẩn tuân thủ:** Clean Architecture + RESTful Conventions (`AGENTS.md` Pattern 6 & 7)  
> **Phương pháp hướng dẫn:** **Không sinh code mẫu sẵn**. Cung cấp đầy đủ cấu trúc 5W1H, kiến trúc giải pháp, đề xuất kỹ thuật và hướng dẫn từng bước logic (Step-by-Step Blueprint) để Tech Lead tự gõ mã nguồn.

---

## 💡 ĐỀ XUẤT KIẾN TRÚC TỪ CỐ VẤN KỸ THUẬT (ARCHITECT'S PROPOSALS)

Trước khi bắt tay vào xây dựng Controller, tôi xin đề xuất 3 giải pháp kiến trúc để đảm bảo tính chuẩn hóa cao nhất:

### Đề xuất 1: Tách Biệt Request DTOs Của Tầng API Khỏi Commands Của Tầng Application
* **Vấn đề:** Trong RESTful URL, mã hội thoại nằm trên route: `/api/v1/chat/conversations/{id}/messages/text`. Nếu dùng trực tiếp `SendTextMessageCommand` làm body, client sẽ phải gửi dư thừa thuộc tính `ConversationId` trong JSON body.
* **Đề xuất:** Tạo riêng thư mục `Contracts/V1/Chat/Requests/` chứa các record mỏng:
  * `SendTextMessageRequest(string Content)`
  * `SendVoiceMessageRequest(string VoiceUrl, int VoiceDuration)`
  * `LogCallSessionRequest(CallStatus Status, int DurationSeconds, CallDirection Direction)`
  * `UploadAudioResponse(string VoiceUrl)`
* Controller sẽ nhận `id` từ Route parameter và các trường còn lại từ Body, sau đó map sang Command của Application để gửi qua MediatR.

### Đề xuất 2: Chuẩn Hóa Endpoint Upload File Âm Thanh (`POST /api/v1/chat/voice`)
* File ghi âm từ trình duyệt gửi lên dưới dạng `multipart/form-data` (`IFormFile`).
* Cần kiểm tra chặt chẽ:
  1. File không được rỗng (`Length > 0`).
  2. Kích thước file tối đa: **10MB** (tin nhắn thoại tối đa 120s chỉ nặng tầm 500KB - 2MB, giới hạn 10MB để chặn tấn công từ chối dịch vụ DoS/băng thông).
  3. Định dạng hợp lệ (MIME types): `audio/webm`, `audio/wav`, `audio/ogg`, `audio/mp3`, `audio/mpeg`.
* Sau khi kiểm tra hợp lệ, Controller mở stream từ `file.OpenReadStream()` $\rightarrow$ gọi `IAudioStorageService.SaveAudioAsync(...)` $\rightarrow$ trả về `voiceUrl` cho client.

### Đề xuất 3: Tuân Thủ Triệt Để Chuẩn HTTP Verbs Trong `AGENTS.md`
* Thao tác đánh dấu đã đọc (`/api/v1/chat/conversations/{id}/read`) làm thay đổi trạng thái của tin nhắn và hội thoại $\rightarrow$ Bắt buộc sử dụng verb **`PUT`** (Pattern 7: Mọi state-changing mutation dùng `PUT`, tuyệt đối không dùng `PATCH`).

---

## 📌 MỤC LỤC TRÌNH TỰ CÁC FILE CẦN TẠO

```text
src/Services/Tripory/Tripory.API/
├── Contracts/V1/Chat/
│   ├── Requests/
│   │   ├── SendTextMessageRequest.cs      # [NEW] DTO nhận nội dung tin nhắn văn bản
│   │   ├── SendVoiceMessageRequest.cs     # [NEW] DTO nhận URL và thời lượng tin nhắn thoại
│   │   └── LogCallSessionRequest.cs       # [NEW] DTO nhận thông số cuộc gọi thoại
│   └── Responses/
│       └── UploadAudioResponse.cs         # [NEW] DTO trả về đường dẫn file âm thanh vừa upload
└── Controllers/V1/
    └── ChatController.cs                  # [NEW] Controller quản lý toàn bộ 8 REST Endpoints của Chat
```

---

## 📦 PHẦN 1: CÁC REQUEST/RESPONSE CONTRACTS (TẦNG API)

### 1.1 `SendTextMessageRequest.cs`
* **Why:** Nhận payload văn bản người dùng soạn thảo từ bàn phím.
* **What:** C# Record chứa duy nhất 1 thuộc tính `string Content`.
* **Who:** Tầng `Tripory.API/Contracts/V1/Chat/Requests/`.
* **Where:** `src/Services/Tripory/Tripory.API/Contracts/V1/Chat/Requests/SendTextMessageRequest.cs`
* **When:** Được deserialize khi client gửi HTTP POST `/api/v1/chat/conversations/{id}/messages/text`.
* **How (Các bước viết code):**
  * **Bước 1:** Khai báo namespace `Tripory.API.Contracts.V1.Chat.Requests;`.
  * **Bước 2:** Định nghĩa `public record SendTextMessageRequest(string Content);`.

---

### 1.2 `SendVoiceMessageRequest.cs`
* **Why:** Nhận thông tin tin nhắn thoại sau khi client đã upload file âm thanh thành công lên server.
* **What:** C# Record chứa `string VoiceUrl` và `int VoiceDuration`.
* **Where:** `src/Services/Tripory/Tripory.API/Contracts/V1/Chat/Requests/SendVoiceMessageRequest.cs`
* **When:** Khi client gửi HTTP POST `/api/v1/chat/conversations/{id}/messages/voice`.
* **How (Các bước viết code):**
  * **Bước 1:** Khai báo namespace `Tripory.API.Contracts.V1.Chat.Requests;`.
  * **Bước 2:** Định nghĩa `public record SendVoiceMessageRequest(string VoiceUrl, int VoiceDuration);`.

---

### 1.3 `LogCallSessionRequest.cs`
* **Why:** Nhận dữ liệu tổng kết cuộc gọi thoại (PT-03) sau khi kết thúc đàm thoại để lưu vết `call_log`.
* **What:** C# Record chứa `CallStatus Status`, `int DurationSeconds`, `CallDirection Direction`.
* **Where:** `src/Services/Tripory/Tripory.API/Contracts/V1/Chat/Requests/LogCallSessionRequest.cs`
* **How (Các bước viết code):**
  * **Bước 1:** Import `using Tripory.Domain.Enums;`.
  * **Bước 2:** Khai báo namespace `Tripory.API.Contracts.V1.Chat.Requests;`.
  * **Bước 3:** Định nghĩa record `LogCallSessionRequest(CallStatus Status, int DurationSeconds, CallDirection Direction);`.

---

### 1.4 `UploadAudioResponse.cs`
* **Why:** Chuẩn hóa dữ liệu trả về sau khi upload file ghi âm giọng nói thành công.
* **What:** C# Record chứa thuộc tính `string VoiceUrl`.
* **Where:** `src/Services/Tripory/Tripory.API/Contracts/V1/Chat/Responses/UploadAudioResponse.cs`
* **How (Các bước viết code):**
  * **Bước 1:** Khai báo namespace `Tripory.API.Contracts.V1.Chat.Responses;`.
  * **Bước 2:** Định nghĩa `public record UploadAudioResponse(string VoiceUrl);`.

---

## 🎮 PHẦN 2: THIẾT KẾ CHI TIẾT `ChatController.cs`

* **Why (Tại sao):** Đóng vai trò là HTTP Gateway tiếp nhận toàn bộ các tương tác RESTful của module chat, chuyển tiếp xử lý vào MediatR Pipeline, ghi log nghiệp vụ, và đóng gói dữ liệu phản hồi theo chuẩn `ApiResponse<T>`.
* **What (Là cái gì):** Controller kế thừa từ `ApiController`, gắn `[Authorize]` bắt buộc đăng nhập cho toàn bộ endpoints.
* **Who:** Tầng `Tripory.API/Controllers/V1/`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.API/Controllers/V1/ChatController.cs`
* **When (Khi nào gọi):** Bất cứ khi nào Frontend cần tải danh sách hộp thư, tải lịch sử chat, gửi tin nhắn hoặc upload file ghi âm.

---

### 🏗️ Hướng Dẫn Từng Bước Viết `ChatController.cs`

#### Bước 1: Khai báo Namespace & Kế Thừa
* Import các thư viện cần thiết:
  * `MediatR` (`ISender`)
  * `Microsoft.AspNetCore.Authorization` (`[Authorize]`)
  * `Microsoft.AspNetCore.Mvc` (`[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[FromRoute]`, `[FromBody]`, `[FromQuery]`)
  * `Tripory.API.Common.Responses` (`ApiResponse`, `ApiResponse<T>`)
  * `Tripory.API.Contracts.V1.Chat.Requests` & `Responses`
  * `Tripory.Application.Abstractions.Storage` (`IAudioStorageService`)
  * `Tripory.Application.UseCases.V1.Chat.Commands`
  * `Tripory.Application.UseCases.V1.Chat.Queries`
  * `Tripory.Application.UseCases.V1.Chat.Responses`
* Khai báo class `public class ChatController : ApiController`.
* Gắn attribute `[Authorize]` trên đầu class.

#### Bước 2: Khai báo Constructor & Dependencies
* Inject 3 dependencies thông qua Constructor:
  1. `ISender sender` (truyền xuống `base(sender)`).
  2. `IAudioStorageService audioStorageService` (lưu vào biến `private readonly`).
  3. `ILogger<ChatController> logger` (lưu vào biến `private readonly`).

---

#### Bước 3: Endpoint 1 – Lấy danh sách hội thoại của người dùng
* **Route:** `[HttpGet("conversations")]`
* **Swagger Documentation:** Gắn `[ProducesResponseType]` cho mã `200 OK` (kiểu `ApiResponse<IReadOnlyList<ConversationDto>>`) và `401 Unauthorized`.
* **Logic xử lý từng bước:**
  1. Ghi log: `_logger.LogInformation("Lấy danh sách các cuộc hội thoại của người dùng hiện tại.");`.
  2. Khởi tạo `new GetConversationsQuery()`.
  3. Gửi qua MediatR: `var result = await Sender.Send(query, ct);`.
  4. Kiểm tra: `if (result.IsFailure) return HandlerFailure(result);`.
  5. Trả về: `return Ok(ApiResponse<IReadOnlyList<ConversationDto>>.Success(result.Value));`.

---

#### Bước 4: Endpoint 2 – Phân trang lịch sử tin nhắn trong hội thoại
* **Route:** `[HttpGet("conversations/{id:guid}/messages")]`
* **Parameters:** `[FromRoute] Guid id`, `[FromQuery] int page = 1`, `[FromQuery] int pageSize = 30`, `CancellationToken ct`.
* **Swagger Documentation:** Gắn `[ProducesResponseType]` cho mã `200 OK` (kiểu `ApiResponse<IReadOnlyList<ChatMessageDto>>`), `400 BadRequest`, `403 Forbidden`, `404 NotFound`.
* **Logic xử lý từng bước:**
  1. Ghi log kèm `ConversationId`, `Page`, `PageSize`.
  2. Khởi tạo `new GetMessagesQuery(id, page, pageSize)`.
  3. Gửi qua MediatR: `var result = await Sender.Send(query, ct);`.
  4. Nếu `result.IsFailure` thì gọi `HandlerFailure(result)`.
  5. Thành công thì trả về `Ok(ApiResponse<IReadOnlyList<ChatMessageDto>>.Success(result.Value));`.

---

#### Bước 5: Endpoint 3 – Mở hoặc tạo mới hội thoại 1-1 với bạn bè
* **Route:** `[HttpPost("conversations/{partnerId:guid}")]`
* **Parameters:** `[FromRoute] Guid partnerId`, `CancellationToken ct`.
* **Swagger Documentation:** Gắn `[ProducesResponseType]` cho mã `200 OK` (kiểu `ApiResponse<ConversationDto>`), `400 BadRequest`, `404 NotFound`.
* **Logic xử lý từng bước:**
  1. Ghi log: Yêu cầu mở/tạo hội thoại với PartnerId.
  2. Khởi tạo `new GetOrCreateConversationCommand(partnerId)`.
  3. Gửi qua MediatR $\rightarrow$ kiểm tra `result.IsFailure`.
  4. Trả về `Ok(ApiResponse<ConversationDto>.Success(result.Value));`.

---

#### Bước 6: Endpoint 4 – Gửi tin nhắn văn bản
* **Route:** `[HttpPost("conversations/{id:guid}/messages/text")]`
* **Parameters:** `[FromRoute] Guid id`, `[FromBody] SendTextMessageRequest request`, `CancellationToken ct`.
* **Swagger Documentation:** Gắn `[ProducesResponseType]` cho mã `200 OK` (kiểu `ApiResponse<ChatMessageDto>`), `400 BadRequest`, `403 Forbidden`, `404 NotFound`.
* **Logic xử lý từng bước:**
  1. Ghi log nhận yêu cầu gửi tin nhắn text vào hội thoại `id`.
  2. Map dữ liệu thành `new SendTextMessageCommand(id, request.Content)`.
  3. Gửi qua MediatR $\rightarrow$ kiểm tra `result.IsFailure` $\rightarrow$ `HandlerFailure(result)`.
  4. Trả về `Ok(ApiResponse<ChatMessageDto>.Success(result.Value, "Gửi tin nhắn thành công."));`.

---

#### Bước 7: Endpoint 5 – Gửi tin nhắn thoại
* **Route:** `[HttpPost("conversations/{id:guid}/messages/voice")]`
* **Parameters:** `[FromRoute] Guid id`, `[FromBody] SendVoiceMessageRequest request`, `CancellationToken ct`.
* **Swagger Documentation:** Gắn `[ProducesResponseType]` cho mã `200 OK` (kiểu `ApiResponse<ChatMessageDto>`), `400 BadRequest`, `403 Forbidden`, `404 NotFound`.
* **Logic xử lý từng bước:**
  1. Ghi log nhận yêu cầu gửi voice message vào hội thoại `id` với thời lượng `request.VoiceDuration`.
  2. Map thành `new SendVoiceMessageCommand(id, request.VoiceUrl, request.VoiceDuration)`.
  3. Gửi qua MediatR $\rightarrow$ kiểm tra `result.IsFailure`.
  4. Trả về `Ok(ApiResponse<ChatMessageDto>.Success(result.Value, "Gửi tin nhắn thoại thành công."));`.

---

#### Bước 8: Endpoint 6 – Upload file âm thanh thu từ Microphone
* **Route:** `[HttpPost("voice")]`
* **Content-Type:** `multipart/form-data`
* **Parameters:** `IFormFile file`, `CancellationToken ct`.
* **Swagger Documentation:** Gắn `[Consumes("multipart/form-data")]`, `[ProducesResponseType]` cho mã `200 OK` (kiểu `ApiResponse<UploadAudioResponse>`), `400 BadRequest`.
* **Logic xử lý từng bước:**
  1. **Kiểm tra rỗng:** Nếu `file == null` hoặc `file.Length == 0` $\rightarrow$ trả về `BadRequest(ApiResponse.Failure("File.Empty", "File âm thanh không được để trống."));`.
  2. **Kiểm tra dung lượng:** Nếu `file.Length > 10 * 1024 * 1024` (10MB) $\rightarrow$ trả về `BadRequest(ApiResponse.Failure("File.TooLarge", "Dung lượng file ghi âm không được vượt quá 10MB."));`.
  3. **Kiểm tra định dạng (MIME validation):**
     * Danh sách cho phép: `audio/webm`, `audio/wav`, `audio/wave`, `audio/x-wav`, `audio/ogg`, `audio/mp3`, `audio/mpeg`.
     * Nếu ContentType của file không nằm trong danh sách $\rightarrow$ trả về `BadRequest(ApiResponse.Failure("File.InvalidFormat", "Định dạng file âm thanh không được hỗ trợ. Chỉ chấp nhận webm, wav, ogg, mp3."));`.
  4. **Thực hiện lưu file:**
     * Mở stream: `await using var stream = file.OpenReadStream();`.
     * Gọi lưu file: `var voiceUrl = await _audioStorageService.SaveAudioAsync(stream, file.FileName, file.ContentType, ct);`.
  5. **Trả kết quả:**
     * `return Ok(ApiResponse<UploadAudioResponse>.Success(new UploadAudioResponse(voiceUrl), "Tải lên file âm thanh thành công."));`.

---

#### Bước 9: Endpoint 7 – Đánh dấu đã xem toàn bộ tin nhắn trong hội thoại
* **Route:** `[HttpPut("conversations/{id:guid}/read")]`
* **Parameters:** `[FromRoute] Guid id`, `CancellationToken ct`.
* **Swagger Documentation:** Gắn `[ProducesResponseType]` cho mã `200 OK` (kiểu `ApiResponse<object>`), `400 BadRequest`, `403 Forbidden`, `404 NotFound`.
* **Logic xử lý từng bước:**
  1. Ghi log yêu cầu đánh dấu đã đọc cho hội thoại `id`.
  2. Khởi tạo `new MarkConversationAsReadCommand(id)`.
  3. Gửi qua MediatR $\rightarrow$ kiểm tra `result.IsFailure` $\rightarrow$ `HandlerFailure(result)`.
  4. Trả về: `return Ok(ApiResponse.Success("Đã đánh dấu đã đọc cuộc hội thoại."));`.

---

#### Bước 10: Endpoint 8 – Ghi vết nhật ký cuộc gọi thoại vào dòng chat
* **Route:** `[HttpPost("conversations/{id:guid}/call-log")]`
* **Parameters:** `[FromRoute] Guid id`, `[FromBody] LogCallSessionRequest request`, `CancellationToken ct`.
* **Swagger Documentation:** Gắn `[ProducesResponseType]` cho mã `200 OK` (kiểu `ApiResponse<ChatMessageDto>`), `400 BadRequest`, `404 NotFound`.
* **Logic xử lý từng bước:**
  1. Ghi log nhận yêu cầu ghi vết cuộc gọi (Status, Duration).
  2. Map thành `new LogCallSessionCommand(id, request.Status, request.DurationSeconds, request.Direction)`.
  3. Gửi qua MediatR $\rightarrow$ kiểm tra `result.IsFailure`.
  4. Trả về `Ok(ApiResponse<ChatMessageDto>.Success(result.Value, "Ghi nhận nhật ký cuộc gọi thành công."));`.

---

## 🎯 LỆNH BUILD KIỂM CHỨNG (VERIFICATION)

Sau khi anh hoàn thành việc viết 4 file Contracts và `ChatController.cs`, chạy lệnh:

```bash
dotnet build src/Services/Tripory/Tripory.API/Tripory.API.csproj
```

**Tiêu chuẩn nghiệm thu:** Lệnh trả về:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Không có bất kỳ cảnh báo hoặc lỗi biên dịch nào!
