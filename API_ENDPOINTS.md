# API_ENDPOINTS.md – Tổng Hợp API Hiện Có Của Tripory Backend

> **Cập nhật:** 09/10/2026  
> **Base URL (local):** `http://localhost:5251` · `https://localhost:7071`  
> **Định dạng response REST:** Mọi endpoint trả về `ApiResponse<T>` (thành công) hoặc `ApiResponse<object>` (thất bại, kèm `errorCode` + `message`).  
> **Xác thực:** 🔒 = cần header `Authorization: Bearer <accessToken>` · 🌐 = public.

---

## 1. Auth – `/api/v1/auth`

| Verb | Route | Auth | Body | Mô tả |
|---|---|:-:|---|---|
| POST | `/register` | 🌐 | `RegisterCommand` | Đăng ký tài khoản |
| POST | `/login` | 🌐 | `LoginCommand` | Đăng nhập, trả về access + refresh token |
| POST | `/refresh-token` | 🌐 | `RefreshTokenCommand` | Cấp lại cặp token mới |
| PUT | `/change-password` | 🔒 | `ChangePasswordCommand` | Đổi mật khẩu |

---

## 2. Users – `/api/v1/users`

| Verb | Route | Auth | Body / Query | Mô tả |
|---|---|:-:|---|---|
| GET | `/` | 🌐 | `?search=&limit=50` | Danh sách / tìm kiếm người dùng |
| GET | `/me` | 🔒 | — | Profile người dùng hiện tại |
| PUT | `/profile` | 🔒 | `UpdateUserProfileCommand` | Cập nhật profile |

---

## 3. Chat – `/api/v1/chat` (tất cả 🔒)

| Verb | Route | Body / Query | Mô tả |
|---|---|---|---|
| GET | `/conversations` | — | Danh sách hội thoại |
| POST | `/conversations/{partnerId}` | — | Lấy hoặc tạo hội thoại 1-1 |
| GET | `/conversations/{id}/messages` | `?page=1&pageSize=30` | Tin nhắn trong hội thoại (phân trang) |
| POST | `/conversations/{id}/messages/text` | `SendTextMessageRequest` | Gửi tin nhắn văn bản |
| POST | `/conversations/{id}/messages/voice` | `SendVoiceMessageRequest` | Gửi tin nhắn thoại (`voiceUrl` lấy từ `POST /voice`) |
| POST | `/voice` | `multipart/form-data` (`file`) | Upload file ghi âm (≤ 10MB; webm/wav/ogg/mp3) |
| POST | `/conversations/{id}/call-log` | `LogCallSessionRequest` | Ghi nhật ký cuộc gọi |
| PUT | `/conversations/{id}/read` | — | Đánh dấu hội thoại đã đọc |

---

## 4. SignalR Hub – `/hubs/chat` (🔒)

### Client → Server

| Method | Tham số | Mô tả |
|---|---|---|
| `JoinConversation` | `conversationId` | Tham gia group hội thoại |
| `LeaveConversation` | `conversationId` | Rời group hội thoại |
| `CallUser` | `targetUserId, offer` | Bắt đầu gọi thoại (WebRTC offer) |
| `AcceptCall` | `callerUserId, answer` | Chấp nhận cuộc gọi (WebRTC answer) |
| `RejectCall` | `callerUserId, reason` | Từ chối cuộc gọi |
| `EndCall` | `partnerUserId` | Kết thúc cuộc gọi |
| `SendIceCandidate` | `targetUserId, candidate` | Trao đổi ICE candidate |

### Server → Client

| Nhóm | Events |
|---|---|
| Tin nhắn | `ReceiveMessage`, `ReceiveConversationMessage`, `MessageRead`, `ConversationRead`, `ConversationUpdated` |
| Cuộc gọi | `IncomingCall`, `CallAccepted`, `CallRejected`, `CallEnded`, `ReceiveIceCandidate` |

---

## 5. Static Files

* `UseStaticFiles` phục vụ các file ghi âm đã upload (URL trả về từ `POST /api/v1/chat/voice`).

---

## 6. Itinerary – `/api/v1/itineraries` (🔒 trừ `GET /{id}`)

Controller: `ItinerariesController` (`[Authorize]` cấp class). Body dùng Request contract tại `Contracts/V1/Itineraries/Requests`, map 1-1 sang Command/Query trong `Tripory.Application/UseCases/V1/Itineraries`. Route params có constraint: `{id:guid}`, `{waypointId:guid}`, `{dayNumber:int}`.

| Verb | Route | Auth | Body / Query | Command / Query | Response `data` |
|---|---|:-:|---|---|---|
| POST | `/` | 🔒 | `CreateItineraryRequest { title }` | `CreateQuickDraftItineraryCommand` | `ItineraryDetailDto` (**201 Created** + header `Location`) |
| GET | `/mine` | 🔒 | `?pageIndex=1&pageSize=10&isPublic=` | `GetMyItinerariesQuery` | `PagedResult<ItinerarySummaryDto>` |
| GET | `/{id}` | 🌐 | — | `GetItineraryByIdQuery` | `ItineraryDetailDto` (gồm `days[]`, `waypoints[]`); 403 nếu hành trình không public |
| PUT | `/{id}` | 🔒 | `UpdateItineraryMetadataRequest { title, description?, startDate?, coverImageUrl? }` | `UpdateItineraryMetadataCommand` | `{}` |
| PUT | `/{id}/publish` | 🔒 | — | `PublishItineraryCommand` | `{}` |
| DELETE | `/{id}` | 🔒 | — | `DeleteItineraryCommand` | `{}` |
| POST | `/{id}/waypoints` | 🔒 | `AddWaypointRequest { dayNumber, name, address?, longitude, latitude, notes? }` | `AddWaypointCommand` | `WaypointDto` |
| PUT | `/{id}/waypoints/{waypointId}` | 🔒 | `UpdateWaypointRequest { name, address?, longitude, latitude, notes? }` | `UpdateWaypointCommand` | `{}` |
| DELETE | `/{id}/waypoints/{waypointId}` | 🔒 | — | `DeleteWaypointCommand` | `{}` |
| PUT | `/{id}/days/{dayNumber}/reorder-waypoints` | 🔒 | `ReorderWaypointsRequest { orderedWaypointIds: Guid[] }` | `ReorderWaypointsCommand` | `{}` |
| PUT | `/{id}/days/{dayNumber}/subtitle` | 🔒 | `SetDaySubtitleRequest { subtitle? }` | `SetDaySubtitleCommand` | `{}` |

> Trừ `POST /` trả 201, các endpoint còn lại trả 200 khi thành công. Ảnh bìa hiện gửi dạng URL string qua `coverImageUrl`; chưa có endpoint upload ảnh bìa.

---

## 7. Response Envelope & Mã Lỗi

`ApiResponse` (`Common/Responses/ApiResponse.cs`) gồm các field: `status`, `data`, `message`, `errorCode`, `timestamp`. Endpoint không có dữ liệu trả về `data: {}`.

`ApiController.HandlerFailure` map lỗi sang HTTP status:

| Loại lỗi | HTTP |
|---|---|
| `NotFound` | 404 |
| `Conflict` / `AlreadyExists` | 409 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| Còn lại (validation, business rule…) | 400 |

---

## 8. Ghi Chú Không Đồng Nhất

* `AuthController` và `UsersController` bind body trực tiếp vào Command (`[FromBody] RegisterCommand`), trong khi `ChatController` và `ItinerariesController` dùng Request contract riêng (`Contracts/V1/*/Requests`). Nên chuyển Auth/Users theo cùng cách.
* `ChatController.UploadVoiceMessage` thiếu `_logger.LogInformation(...)` đầu action (vi phạm AGENTS.md §6).
