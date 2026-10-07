# API_ENDPOINTS.md – Tổng Hợp API Hiện Có Của Tripory Backend

> **Cập nhật:** 05/10/2026  
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

## 6. Itinerary – `/api/v1/itineraries` ⏳ (Chưa có Controller)

Tầng Application đã có đủ Command/Query; Controller sẽ triển khai theo [GUIDE_STEP_05_API_LAYER.md](GUIDE_STEP_05_API_LAYER.md).

| Verb | Route (dự kiến) | Auth | Command / Query |
|---|---|:-:|---|
| POST | `/` | 🔒 | `CreateQuickDraftItineraryCommand` |
| GET | `/mine` | 🔒 | `GetMyItinerariesQuery` |
| GET | `/{id}` | 🌐 | `GetItineraryByIdQuery` |
| PUT | `/{id}` | 🔒 | `UpdateItineraryMetadataCommand` |
| PUT | `/{id}/publish` | 🔒 | `PublishItineraryCommand` |
| DELETE | `/{id}` | 🔒 | `DeleteItineraryCommand` |
| POST | `/{id}/waypoints` | 🔒 | `AddWaypointCommand` |
| PUT | `/{id}/waypoints/{waypointId}` | 🔒 | `UpdateWaypointCommand` |
| DELETE | `/{id}/waypoints/{waypointId}` | 🔒 | `DeleteWaypointCommand` |
| PUT | `/{id}/days/{dayNumber}/reorder-waypoints` | 🔒 | `ReorderWaypointsCommand` |
| PUT | `/{id}/days/{dayNumber}/subtitle` | 🔒 | `SetDaySubtitleCommand` |

---

## 7. Ghi Chú Không Đồng Nhất

* `AuthController` và `UsersController` bind body trực tiếp vào Command (`[FromBody] RegisterCommand`), trong khi `ChatController` dùng Request contract riêng (`Contracts/V1/Chat/Requests`). Nên thống nhất theo cách của Chat.
