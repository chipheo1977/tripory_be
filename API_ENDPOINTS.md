# API_ENDPOINTS.md – Tổng Hợp API Hiện Có Của Tripory Backend

> **Cập nhật:** 13/11/2026 (thêm like và comment lịch trình, Phase 5.1)  
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
| POST | `/logout` | 🔒 | `LogoutCommand` | Đăng xuất: thu hồi 1 refresh token của chính user |
| PUT | `/change-password` | 🔒 | `ChangePasswordCommand` | Đổi mật khẩu |

### `POST /api/v1/auth/refresh-token`
AccessToken (có thể đã hết hạn) sai chữ ký, sai định dạng hoặc ký bằng khóa khác → `401` với `errorCode` = `Token.UnauthorizedAccessToken` (FE nên xóa phiên và yêu cầu đăng nhập lại). Các mã `Token.*` khác giữ nguyên.

### `POST /api/v1/auth/logout`

Body:

```json
{ "refreshToken": "<refresh token cần thu hồi>" }
```

* Cần header `Authorization: Bearer <accessToken>`. Thiếu/sai access token → `401`.
* Chỉ thu hồi **đúng token được gửi lên** và **chỉ khi token thuộc về user đang đăng nhập**. Không thu hồi token/thiết bị khác, không kích hoạt cơ chế Token Reuse Detection (`Token.Compromised`).
* Idempotent: token không tồn tại / đã thu hồi / đã hết hạn / thuộc user khác đều trả về **cùng một response thành công** (`200`, message "Đăng xuất thành công.", `data: {}`), nên không lộ token có tồn tại hay của ai.
* `refreshToken` rỗng hoặc dài quá 512 ký tự → `400` (validation).
* Lưu ý: sau khi logout, nếu client vẫn gọi `/refresh-token` với token đó thì sẽ bị coi là tái sử dụng token đã thu hồi (`Token.Compromised`, thu hồi toàn bộ phiên của user). Client nên xóa token ngay sau khi logout.
* Endpoint chỉ bổ sung thêm; client cũ không gọi vẫn hoạt động bình thường. Không dùng rate-limit policy auth.

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

## 6. Itinerary – `/api/v1/itineraries` (🔒 trừ `GET /{id}`, `GET /{id}/like`, `GET /{id}/comments`)

Controller: `ItinerariesController` (`[Authorize]` cấp class; action like và comment nằm ở file partial `ItinerariesController.Social.cs`, cùng route). Body dùng Request contract tại `Contracts/V1/Itineraries/Requests`, map 1-1 sang Command/Query trong `Tripory.Application/UseCases/V1/Itineraries`. Route params có constraint: `{id:guid}`, `{waypointId:guid}`, `{commentId:guid}`, `{dayNumber:int}`.

| Verb | Route | Auth | Body / Query | Command / Query | Response `data` |
|---|---|:-:|---|---|---|
| POST | `/` | 🔒 | `CreateItineraryRequest { title, destinationCode, dayCount?, startDate?, interests? }` | `CreateQuickDraftItineraryCommand` | `ItineraryDetailDto` (**201 Created** + header `Location`); DTO có thêm `coverImages`, `selectedCoverIndex`, `destinationCode`, `interests`; `likeCount`, `commentCount`, `likedByMe` luôn là `0`, `0`, `false` |
| GET | `/mine` | 🔒 | `?pageIndex=1&pageSize=10&isPublic=` | `GetMyItinerariesQuery` | `PagedResult<ItinerarySummaryDto>` |
| GET | `/{id}` | 🌐 | — | `GetItineraryByIdQuery` | `ItineraryDetailDto` (gồm `days[]`, `waypoints[]`, `likeCount`, `commentCount`, `likedByMe`); 403 `Itinerary.Forbidden` nếu là bản nháp và người gọi không phải chủ |
| PUT | `/{id}` | 🔒 | `UpdateItineraryMetadataRequest { title, description?, startDate?, coverImages?, selectedCoverIndex?, destinationCode?, interests? }` (`destinationCode`, `interests`, `coverImages` thiếu hoặc `null` thì giữ nguyên giá trị đã lưu; `coverImages: []` xóa hết ảnh bìa) | `UpdateItineraryMetadataCommand` | `{}` |
| PUT | `/{id}/publish` | 🔒 | — | `PublishItineraryCommand` | `{}` |
| DELETE | `/{id}` | 🔒 | — | `DeleteItineraryCommand` | `{}` |
| POST | `/{id}/waypoints` | 🔒 | `AddWaypointRequest { dayNumber, name, address?, longitude, latitude, notes? }` | `AddWaypointCommand` | `WaypointDto` |
| PUT | `/{id}/waypoints/{waypointId}` | 🔒 | `UpdateWaypointRequest { name, address?, longitude, latitude, notes? }` | `UpdateWaypointCommand` | `{}` |
| DELETE | `/{id}/waypoints/{waypointId}` | 🔒 | — | `DeleteWaypointCommand` | `{}` |
| PUT | `/{id}/days/{dayNumber}/reorder-waypoints` | 🔒 | `ReorderWaypointsRequest { orderedWaypointIds: Guid[] }` | `ReorderWaypointsCommand` | `{}` |
| PUT | `/{id}/days/{dayNumber}/subtitle` | 🔒 | `SetDaySubtitleRequest { subtitle? }` | `SetDaySubtitleCommand` | `{}` |
| PUT | `/{id}/like` | 🔒 | — | `LikeItineraryCommand` | `LikeStateDto` |
| DELETE | `/{id}/like` | 🔒 | — | `UnlikeItineraryCommand` | `LikeStateDto` |
| GET | `/{id}/like` | 🌐 | — | `GetLikeStateQuery` | `LikeStateDto` |
| GET | `/{id}/comments` | 🌐 | `?pageIndex=1&pageSize=10` | `GetCommentsQuery` | `PagedResult<CommentDto>` |
| POST | `/{id}/comments` | 🔒 | `AddCommentRequest { content }` | `AddCommentCommand` | `CommentDto` (**201 Created**, không có header `Location`) |
| DELETE | `/{id}/comments/{commentId}` | 🔒 | — | `DeleteCommentCommand` | `{}` |

> Trừ `POST /` và `POST /{id}/comments` trả 201, các endpoint còn lại trả 200 khi thành công. Ảnh bìa là danh sách tối đa 5 URL `https` (mỗi URL tối đa 500 ký tự, không trùng) gửi qua `coverImages` cùng `selectedCoverIndex`; `coverImageUrl` trong response là ảnh đang hiển thị. Chưa có endpoint upload ảnh bìa.

### Ba trường like/comment của `ItineraryDetailDto`

Thêm ở cuối DTO, các trường cũ giữ nguyên:

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `likeCount` | int | Tổng số like, đếm trực tiếp |
| `commentCount` | int | Tổng số comment, đếm trực tiếp |
| `likedByMe` | bool | Người gọi đã like chưa; khách (không có Bearer) luôn `false` |

* `GET /{id}` của lịch trình đã xuất bản: trả số liệu thật.
* `GET /{id}` của bản nháp (chủ xem) và `POST /`: trả `0`, `0`, `false`.

### Like và comment (`/{id}/like`, `/{id}/comments`)

Chỉ áp dụng cho lịch trình đã xuất bản. Bản nháp trả `403 Social.DraftForbidden` cho mọi người gọi, kể cả chủ. Lịch trình không tồn tại (hoặc đã xóa mềm) trả `404 Itinerary.NotFound`.

**Thứ tự kiểm lỗi chung:** `429` (rate limit, chạy trước xác thực quyền) → `401` (endpoint 🔒, thiếu/sai access token) → `400` validation (chạy trước handler nên có thể ra trước 404) → `404 Itinerary.NotFound` → `403 Social.DraftForbidden` → lỗi riêng của từng endpoint.

**Rate limit:** cửa sổ trượt, phân vùng theo id người dùng (claim `ClaimTypes.NameIdentifier`, khóa `user:{id}`), không có thì theo IP (khóa `ip:{ip}`). Vì vậy `Program.cs` chạy `UseAuthentication` → `UseRateLimiter` → `UseAuthorization`. Bị chặn trả `429` với `errorCode` `RateLimit.TooManyRequests`, message "Bạn thao tác quá nhanh. Vui lòng thử lại sau." và header `Retry-After` (giây; với cửa sổ 60 giây là `10`). Cấu hình qua section `SocialRateLimitOptions` (`Like`, `CommentCreate`, `CommentDelete`). Các policy auth (`auth-login`, `auth-register`, `auth-refresh-token`) không đổi.

| Policy | Áp cho | Mặc định |
|---|---|---|
| `social-like` | `PUT /{id}/like`, `DELETE /{id}/like` | 30 request / 60 giây |
| `social-comment-create` | `POST /{id}/comments` | 5 request / 60 giây |
| `social-comment-delete` | `DELETE /{id}/comments/{commentId}` | 20 request / 60 giây |

`GET /{id}/like` và `GET /{id}/comments` không bị giới hạn.

**`LikeStateDto`:**

```json
{ "likeCount": 13, "likedByMe": true }
```

**`CommentDto`** (`createdAt` là ISO 8601 có offset; `author` là `null` khi người dùng không còn; `author` không có email):

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "content": "Đẹp quá!\nMình sẽ đi thử.",
  "createdAt": "2026-11-13T08:08:08.8812345+00:00",
  "author": {
    "id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
    "displayName": "Trần Thị Lan",
    "avatarUrl": null
  }
}
```

`author.displayName` = `fullName`; nếu `fullName` rỗng hoặc toàn khoảng trắng thì dùng `handle`.

#### `PUT /api/v1/itineraries/{id}/like` 🔒

* Policy `social-like`. Không body.
* Thêm like của người gọi (chủ tự like được). Gọi lặp vẫn `200`, không tạo bản ghi thứ hai.
* `200`, `data` = `LikeStateDto` với `likedByMe: true` và `likeCount` sau thao tác.
* Lỗi: `401`; `404 Itinerary.NotFound`; `403 Social.DraftForbidden`; `429 RateLimit.TooManyRequests`.

#### `DELETE /api/v1/itineraries/{id}/like` 🔒

* Policy `social-like`. Không body.
* Bỏ like nếu có. Chưa like vẫn `200`.
* `200`, `data` = `LikeStateDto` với `likedByMe: false` và `likeCount` sau thao tác.
* Lỗi: như `PUT /{id}/like`.

#### `GET /api/v1/itineraries/{id}/like` 🌐

* Không rate limit. Gửi kèm Bearer thì `likedByMe` theo người gọi; khách luôn `false`.
* `200`, `data` = `LikeStateDto`.
* Lỗi: `404 Itinerary.NotFound`; `403 Social.DraftForbidden`.

#### `GET /api/v1/itineraries/{id}/comments?pageIndex=1&pageSize=10` 🌐

* Không rate limit. `pageIndex` mặc định `1`, phải `>= 1`. `pageSize` mặc định `10`, từ `1` đến `50`.
* Sắp `createdAt` giảm dần, rồi `id` giảm dần.
* `200`, `data` = `PagedResult<CommentDto>`:

```json
{
  "items": [],
  "pageIndex": 1,
  "pageSize": 10,
  "totalCount": 23,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

* Lỗi: `400 Comment.PageInvalid`; `404 Itinerary.NotFound`; `403 Social.DraftForbidden`.

#### `POST /api/v1/itineraries/{id}/comments` 🔒

* Policy `social-comment-create`. Body tối đa 4 KB, lớn hơn bị từ chối trước khi vào handler.

```json
{ "content": "Đẹp quá!\nMình sẽ đi thử." }
```

* `content` được trim; sau trim phải dài 1..500 ký tự (đếm theo `string.Length`). Giữ xuống dòng bên trong, không lọc hay biến đổi HTML.
* `201 Created`, message "Gửi bình luận thành công.", `data` = `CommentDto` vừa tạo (có `author` của người gọi). Không có header `Location`.
* Lỗi: `401`; `400 Comment.ContentRequired` (thiếu, rỗng hoặc toàn khoảng trắng); `400 Comment.ContentTooLong`; `404 Itinerary.NotFound`; `403 Social.DraftForbidden`; `429 RateLimit.TooManyRequests`.

#### `DELETE /api/v1/itineraries/{id}/comments/{commentId}` 🔒

* Policy `social-comment-delete`. Không body.
* Chỉ tác giả comment hoặc chủ lịch trình được xóa. Xóa hẳn (không xóa mềm).
* `200`, message "Xóa bình luận thành công.", `data: {}`.
* Lỗi theo thứ tự: `401`; `404 Itinerary.NotFound`; `403 Social.DraftForbidden`; `404 Comment.NotFound` (không tồn tại, đã xóa hoặc không thuộc lịch trình này); `403 Comment.DeleteForbidden`; `429 RateLimit.TooManyRequests`.

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

Mã lỗi like và comment (`DomainErrors.Social`, `DomainErrors.Comment`):

| `errorCode` | HTTP | Message |
|---|---|---|
| `Social.DraftForbidden` | 403 | Lịch trình này chưa được xuất bản. |
| `Comment.ContentRequired` | 400 | Vui lòng nhập nội dung bình luận. |
| `Comment.ContentTooLong` | 400 | Bình luận tối đa 500 ký tự. |
| `Comment.NotFound` | 404 | Bình luận không còn tồn tại. |
| `Comment.DeleteForbidden` | 403 | Bạn không có quyền xóa bình luận này. |
| `Comment.PageInvalid` | 400 | Tham số phân trang không hợp lệ. |

---

## 8. Ghi Chú Không Đồng Nhất

* `AuthController` và `UsersController` bind body trực tiếp vào Command (`[FromBody] RegisterCommand`), trong khi `ChatController` và `ItinerariesController` dùng Request contract riêng (`Contracts/V1/*/Requests`). Nên chuyển Auth/Users theo cùng cách.
* `ChatController.UploadVoiceMessage` thiếu `_logger.LogInformation(...)` đầu action (vi phạm AGENTS.md §6).
