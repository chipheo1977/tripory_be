# Audit API_ENDPOINTS.md so với code hiện tại

## Kết luận

Chưa cập nhật hoàn toàn. Mục 1–5 (Auth, Users, Chat, SignalR Hub, Static Files) khớp với code: đủ 15 REST endpoint, 7 hub method, 10 server event. Mục 6 (Itinerary) đã lỗi thời. Doc vẫn ghi "⏳ Chưa có Controller" và "Route (dự kiến)", trong khi `ItinerariesController` đã được code đầy đủ 11 endpoint. Verb, route và auth của 11 endpoint này đều đúng với bảng "dự kiến". Phần cần sửa là trạng thái, ngày cập nhật, cột body/query và ghi chú ở mục 7.

## Bằng chứng git

- `API_ENDPOINTS.md` chỉ có 1 commit: `80ee381` (2026-10-07 17:11, HEAD của `dev`). Commit này tạo cùng lúc file doc, `ItinerariesController.cs` (+230 dòng) và 6 Request contract của Itineraries. Doc được viết trước khi controller hoàn thành và không được sửa lại.
- Không có commit nào sau `80ee381`. `git status` sạch, chỉ có `.claude/` chưa track.
- Doc ghi "Cập nhật: 05/10/2026" (dòng 3), sớm hơn ngày commit 07/10/2026.

## Danh sách endpoint thực tế

Route base: `[Route("api/v1/[controller]")]` (`Controllers/ApiController.cs:9`). SignalR: `app.MapHub<ChatHub>("/hubs/chat")` (`Program.cs:133`). Không có minimal API nào.

### Khớp với doc

| Nhóm | Endpoint | Nguồn |
|---|---|---|
| Auth | POST register/login/refresh-token 🌐, PUT change-password 🔒 | `AuthController.cs:20,35,49,63-64` |
| Users | GET `/` 🌐 `?search&limit=50`, GET `/me` 🔒, PUT `/profile` 🔒 | `UsersController.cs:12,23-24,37,51` |
| Chat (🔒 toàn controller) | 8 endpoint, đúng verb, route, query `page=1&pageSize=30`, upload ≤10MB webm/wav/ogg/mp3 | `ChatController.cs:15,53,75,98,116,137,158,178,198` |
| Hub | Join/LeaveConversation, CallUser, AcceptCall, RejectCall, EndCall, SendIceCandidate, `[Authorize]` | `Infrastructure/Hubs/ChatHub.cs:8,51-169` |
| Hub events | ReceiveMessage, ReceiveConversationMessage, MessageRead, ConversationRead, ConversationUpdated | `Infrastructure/Implementations/Realtime/ChatNotificationService.cs:28-67` |
| Static | `app.UseStaticFiles()` | `Program.cs:123` |
| Base URL | 5251 / 7071 | `Properties/launchSettings.json` |

### Sai lệch

| # | Vị trí trong doc | Doc ghi | Code thực tế | Nguồn |
|---|---|---|---|---|
| 1 | Dòng 75–77 | "⏳ Chưa có Controller", Application chờ Controller | Controller đã có, `[Authorize]` ở cấp class | `ItinerariesController.cs:13-14` |
| 2 | Dòng 79 | "Route (dự kiến)" | Route đã chốt, có constraint `{id:guid}`, `{waypointId:guid}`, `{dayNumber:int}` | `ItinerariesController.cs:62,78,99,115,130,151,173,191,211` |
| 3 | Bảng mục 6 | Cột ghi Command/Query, không có Body/Query | Controller bind vào Request contract riêng rồi map sang Command (xem bảng bên dưới) | `Contracts/V1/Itineraries/Requests/*.cs` |
| 4 | GET `/mine` | Không ghi query | `?pageIndex=1&pageSize=10&isPublic=` → `PagedResult<ItinerarySummaryDto>` | `ItinerariesController.cs:41-49` |
| 5 | POST `/` | Không ghi status | Trả **201 Created** kèm header Location (`CreatedAtAction(GetById)`), data là `ItineraryDetailDto`. Các endpoint khác trả 200 | `ItinerariesController.cs:23,34-37` |
| 6 | GET `/{id}` | 🌐 | Đúng là `[AllowAnonymous]`. Nên thêm ghi chú: có thể trả 403 (ProducesResponseType) khi hành trình không public | `ItinerariesController.cs:62-66` |
| 7 | Dòng 3 | 05/10/2026 | Thay đổi gần nhất là 07/10/2026 | git `80ee381` |
| 8 | Mục 7 (dòng 95–97) | Chỉ Chat dùng Request contract | Itineraries cũng đã dùng Request contract. Hiện chỉ còn Auth và Users bind thẳng vào Command | `ItinerariesController.cs:28,82,134,155,195,215` |
| 9 | Toàn doc | Không mô tả response DTO | Không sai, chỉ thiếu. Nên bổ sung cột Response (xem bên dưới) | các `ProducesResponseType` |

Không có endpoint nào nằm trong doc mà không có trong code. Không có endpoint nào nằm trong code mà doc không liệt kê. Bảng "dự kiến" ở mục 6 trùng 100% về verb và route.

### Request/Response của Itineraries

| Verb | Route | Body / Query | Response `data` |
|---|---|---|---|
| POST | `/` | `CreateItineraryRequest { title }` | `ItineraryDetailDto` (201) |
| GET | `/mine` | `?pageIndex=1&pageSize=10&isPublic=` | `PagedResult<ItinerarySummaryDto>` |
| GET | `/{id}` 🌐 | — | `ItineraryDetailDto` (gồm `days[]`, `waypoints[]`) |
| PUT | `/{id}` | `UpdateItineraryMetadataRequest { title, description?, startDate?, coverImageUrl? }` | `{}` |
| PUT | `/{id}/publish` | — | `{}` |
| DELETE | `/{id}` | — | `{}` |
| POST | `/{id}/waypoints` | `AddWaypointRequest { dayNumber, name, address?, longitude, latitude, notes? }` | `WaypointDto` |
| PUT | `/{id}/waypoints/{waypointId}` | `UpdateWaypointRequest { name, address?, longitude, latitude, notes? }` | `{}` |
| DELETE | `/{id}/waypoints/{waypointId}` | — | `{}` |
| PUT | `/{id}/days/{dayNumber}/reorder-waypoints` | `ReorderWaypointsRequest { orderedWaypointIds: Guid[] }` | `{}` |
| PUT | `/{id}/days/{dayNumber}/subtitle` | `SetDaySubtitleRequest { subtitle? }` | `{}` |

Mỗi Request contract map 1-1 sang Command/Query trong `Tripory.Application/UseCases/V1/Itineraries` (Commands/Queries/Responses), không thừa hoặc thiếu field.

## Ghi chú thêm (ngoài phạm vi doc)

- Roadmap `PHASE_03_ITINERARY_PLANNING.md:28,58` nhắc tới upload ảnh bìa multipart ≤10MB (mục 3.4 "Đóng phase"). Trong code không có endpoint này (grep `cover-image|UploadCover|ImageStorage` không có kết quả), nên doc không thiếu. Hiện client gửi `coverImageUrl` dạng string qua `PUT /{id}`.
- `ChatController.UploadVoiceMessage` (`ChatController.cs:158-176`) không gọi `_logger.LogInformation` ở đầu action, trái quy tắc AGENTS.md §6.
- Doc nói chung "mọi endpoint trả về `ApiResponse<T>`". Endpoint không có data trả `data: {}` (`ApiResponse.Success` trong `Common/Responses/ApiResponse.cs`). Cấu trúc envelope gồm `status`, `data`, `message`, `errorCode`, `timestamp`.

## Đề xuất sửa API_ENDPOINTS.md (chưa áp dụng)

1. Dòng 3: đổi thành `> **Cập nhật:** 07/10/2026`.
2. Dòng 75: đổi tiêu đề thành `## 6. Itinerary – /api/v1/itineraries (🔒 trừ GET /{id})`. Bỏ "⏳ (Chưa có Controller)".
3. Dòng 77: xoá câu "Tầng Application đã có đủ Command/Query; Controller sẽ triển khai theo GUIDE_STEP_05…". Có thể thay bằng "Controller: `ItinerariesController`; body dùng Request contract tại `Contracts/V1/Itineraries/Requests`."
4. Thay bảng mục 6 bằng bảng "Request/Response của Itineraries" ở trên: đổi "Route (dự kiến)" thành "Route", thêm cột Body/Query và Response, ghi rõ POST trả 201.
5. Mục 7: sửa thành "`AuthController` và `UsersController` bind body trực tiếp vào Command, còn `ChatController` và `ItinerariesController` dùng Request contract riêng (`Contracts/V1/*/Requests`). Nên chuyển Auth/Users theo cùng cách."
6. (Tuỳ chọn) Thêm mục mô tả envelope `ApiResponse` (`status`, `data`, `message`, `errorCode`, `timestamp`) và quy tắc map lỗi sang HTTP status theo `ApiController.HandlerFailure`: `NotFound`→404, `Conflict`/`AlreadyExists`→409, `Unauthorized`→401, `Forbidden`→403, còn lại→400.
