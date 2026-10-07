# HƯỚNG DẪN TỰ CODE BƯỚC 5: TẦNG API (HTTP GATEWAY & CONTROLLERS)
## Module: ITINERARY_01_MAP+TIMELINE (REST Endpoints Cho Map & Timeline)

> **Dành cho:** Tech Lead / Developer tự tay triển khai code  
> **Vị trí lưu:** `C:\Users\OS\Documents\personal\tripory_be\GUIDE_STEP_05_API_LAYER.md`  
> **Nguyên tắc cốt lõi:** Không sinh code sẵn hàng loạt; cung cấp đặc tả **5W1H**, phân tích **OOP, SOLID, REST & Clean Architecture**, và các bước logic để bạn tự tay lập trình. Controller **chỉ là cổng HTTP mỏng**: nhận request → dựng Command/Query → `Sender.Send` → map `Result` sang HTTP response.  
> **Nguồn đặc tả:** `roadmaps/PHASE_03_ITINERARY_PLANNING.md` (Bước 5) · `../tripory/docs/traveler/ITINERARY_01_MAP+TIMELINE/` · Mẫu tham chiếu có sẵn: `Controllers/V1/ChatController.cs`.

---

## 🗺️ Cây Thư Mục Các File Cần Triển Khai Trong Bước 5

```text
src/Services/Tripory/Tripory.API/
├── Contracts/V1/Itineraries/
│   └── Requests/
│       ├── CreateItineraryRequest.cs            # FILE 01: Body tạo nhanh bản nháp (Title)
│       ├── UpdateItineraryMetadataRequest.cs    # FILE 02: Body cập nhật Title/Description/StartDate/CoverImageUrl
│       ├── AddWaypointRequest.cs                # FILE 03: Body thêm điểm dừng (DayNumber, Name, Lng, Lat...)
│       ├── UpdateWaypointRequest.cs             # FILE 04: Body sửa điểm dừng
│       ├── ReorderWaypointsRequest.cs           # FILE 05: Body danh sách Id theo thứ tự mới
│       └── SetDaySubtitleRequest.cs             # FILE 06: Body phụ đề ngày
│
├── Controllers/V1/
│   └── ItinerariesController.cs                 # FILE 07: 11 endpoints REST /api/v1/itineraries
│
└── Tripory.API.http                             # FILE 08: Kịch bản test thủ công các endpoint (cập nhật)
```

> [!NOTE]
> **Không cần tạo Response contract riêng trong API:** Các DTO `ItineraryDetailDto`, `ItinerarySummaryDto`, `WaypointDto` ở tầng Application đã là hợp đồng đầu ra phẳng, bất biến. Controller bọc chúng trong `ApiResponse<T>` giống hệt `ChatController`.

> [!IMPORTANT]
> **Ngoài phạm vi bước này (Milestone 3.4):** `POST /{id}/cover-image` và `POST /demo-reset` trong roadmap **chưa có Command/Handler** ở tầng Application. Không viết endpoint "chay" gọi thẳng Storage trong Controller — hoàn thiện Application (`UploadCoverImageCommand`, `SeedDemoItineraryCommand`) trước rồi mới mở endpoint.

---

## 📋 Bảng Hợp Đồng Endpoint (API Contract Map)

| # | Verb & Route | Auth | Body | Command / Query | Response thành công |
|:-:|---|:-:|---|---|---|
| 1 | `POST /api/v1/itineraries` | ✅ | `CreateItineraryRequest` | `CreateQuickDraftItineraryCommand` | `201` · `ApiResponse<ItineraryDetailDto>` |
| 2 | `GET /api/v1/itineraries/mine?pageIndex&pageSize&isPublic` | ✅ | — | `GetMyItinerariesQuery` | `200` · `ApiResponse<PagedResult<ItinerarySummaryDto>>` |
| 3 | `GET /api/v1/itineraries/{id}` | 🌐 Anonymous | — | `GetItineraryByIdQuery` | `200` · `ApiResponse<ItineraryDetailDto>` |
| 4 | `PUT /api/v1/itineraries/{id}` | ✅ | `UpdateItineraryMetadataRequest` | `UpdateItineraryMetadataCommand` | `200` · `ApiResponse<object>` |
| 5 | `PUT /api/v1/itineraries/{id}/publish` | ✅ | — | `PublishItineraryCommand` | `200` · `ApiResponse<object>` |
| 6 | `DELETE /api/v1/itineraries/{id}` | ✅ | — | `DeleteItineraryCommand` | `200` · `ApiResponse<object>` |
| 7 | `POST /api/v1/itineraries/{id}/waypoints` | ✅ | `AddWaypointRequest` | `AddWaypointCommand` | `200` · `ApiResponse<WaypointDto>` |
| 8 | `PUT /api/v1/itineraries/{id}/waypoints/{waypointId}` | ✅ | `UpdateWaypointRequest` | `UpdateWaypointCommand` | `200` · `ApiResponse<object>` |
| 9 | `DELETE /api/v1/itineraries/{id}/waypoints/{waypointId}` | ✅ | — | `DeleteWaypointCommand` | `200` · `ApiResponse<object>` |
| 10 | `PUT /api/v1/itineraries/{id}/days/{dayNumber}/reorder-waypoints` | ✅ | `ReorderWaypointsRequest` | `ReorderWaypointsCommand` | `200` · `ApiResponse<object>` |
| 11 | `PUT /api/v1/itineraries/{id}/days/{dayNumber}/subtitle` | ✅ | `SetDaySubtitleRequest` | `SetDaySubtitleCommand` | `200` · `ApiResponse<object>` |

**Bảng ánh xạ lỗi** (đã có sẵn trong `ApiController.HandlerFailure` và `GlobalExceptionHandler`, Controller **không** tự viết lại):

| Nguồn lỗi | Ví dụ mã lỗi | HTTP |
|---|---|:-:|
| `Result.Failure` — code chứa `NotFound` | `Itinerary.NotFound`, `Waypoint.NotFound` | `404` |
| `Result.Failure` — code chứa `Forbidden` | `Itinerary.Forbidden` | `403` |
| `Result.Failure` — code chứa `Unauthorized` | `Auth.Unauthorized` | `401` |
| `Result.Failure` — còn lại / FluentValidation | `Itinerary.CannotPublishEmpty`, `Itinerary.InvalidReorderList` | `400` |
| `DomainException` ném từ Entity/VO | `InvalidItineraryTitleException`, `InvalidCoordinateException` | `400` |

---

## PHẦN 1: REQUEST CONTRACTS (HỢP ĐỒNG ĐẦU VÀO HTTP)

---

### FILE 01 → 06: Nhóm `*Request.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Client (Frontend `tripory` qua axios) gửi JSON body; ASP.NET Core Model Binder deserialize vào record.
* **What (Là gì):** Các `record` bất biến mô tả **đúng phần body** của từng endpoint — không chứa Id lấy từ route.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.API/Contracts/V1/Itineraries/Requests/`.
* **When (Khi nào):** Mỗi request `POST`/`PUT` có body.
* **Why (Tại sao):**
  * **Chống Over-posting / Id Mismatch:** Nếu bind thẳng `UpdateWaypointCommand` từ body, client có thể gửi `ItineraryId` trong body khác với `{id}` trên URL → handler thao tác sai aggregate. Tách Request giúp **route là nguồn sự thật duy nhất** cho định danh.
  * **Tách hợp đồng HTTP khỏi hợp đồng Use Case:** Đổi tên field JSON hay versioning API (V2) không làm vỡ Command của Application.
* **How (Như thế nào):** Controller ghép `route params + request body` → dựng Command.

#### 2. Biểu Diễn OOP, SOLID & Clean Architecture
* **SRP:** Request chỉ mô tả hình dạng dữ liệu đầu vào HTTP; validation nghiệp vụ vẫn nằm ở `*CommandValidator` (FluentValidation pipeline) và Value Object của Domain.
* **Immutability:** Dùng `record` positional — không setter, không logic.
* **Dependency Rule:** Request thuộc tầng API, Application không biết đến chúng.

#### 3. Định Hướng & Tiêu Chuẩn Tự Code
* **Namespace:** `Tripory.API.Contracts.V1.Itineraries.Requests` (file-scoped).
* **Danh sách field (đối chiếu Command tương ứng, bỏ các Id lấy từ route):**

| File | Record | Field |
|---|---|---|
| 01 | `CreateItineraryRequest` | `string Title` |
| 02 | `UpdateItineraryMetadataRequest` | `string Title`, `string? Description`, `DateOnly? StartDate`, `string? CoverImageUrl` |
| 03 | `AddWaypointRequest` | `int DayNumber`, `string Name`, `string? Address`, `double Longitude`, `double Latitude`, `string? Notes` |
| 04 | `UpdateWaypointRequest` | `string Name`, `string? Address`, `double Longitude`, `double Latitude`, `string? Notes` |
| 05 | `ReorderWaypointsRequest` | `IReadOnlyList<Guid> OrderedWaypointIds` |
| 06 | `SetDaySubtitleRequest` | `string? Subtitle` |

* **Lưu ý `DateOnly`:** System.Text.Json (.NET 10) bind sẵn định dạng ISO-8601 `"2026-10-20"` — đúng yêu cầu `PT-01_metadata.md`, không cần custom converter.
* **Lưu ý tọa độ:** Giữ thứ tự **`Longitude` trước `Latitude`** đồng bộ với `BR_01` (`geom: [lng, lat]`) và VO `Wgs84Coordinate`.

---

## PHẦN 2: CONTROLLER

---

### FILE 07: `ItinerariesController.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** HTTP Gateway của module Itinerary; được Frontend gọi ở các màn `ItineraryEditorPage`, `ItineraryListPage`, `PublicItineraryPage`.
* **What (Là gì):** Controller kế thừa `ApiController`, expose 11 endpoint REST theo bảng hợp đồng ở trên.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.API/Controllers/V1/ItinerariesController.cs`. Route gốc tự sinh từ `[Route("api/v1/[controller]")]` của `ApiController` → `/api/v1/itineraries`.
* **When (Khi nào):** Mỗi HTTP request tới `/api/v1/itineraries/**`.
* **Why (Tại sao):** Tuân thủ Pattern 6 & 7 của `AGENTS.md`: Controller mỏng, không business logic, không truy cập dữ liệu trực tiếp; mutation dùng `PUT`, tuyệt đối không `PATCH`.
* **How (Như thế nào):** Constructor nhận `ISender` + `ILogger<ItinerariesController>`; mỗi action làm đúng 4 nhịp:
  1. `_logger.LogInformation(...)` kèm tham số định danh (structured logging).
  2. Dựng Command/Query từ route + body.
  3. `var result = await Sender.Send(command, ct);` → `if (result.IsFailure) return HandlerFailure(result);`
  4. Trả `Ok(ApiResponse<T>.Success(result.Value, "<thông điệp>"))` hoặc `Ok(ApiResponse.Success("<thông điệp>"))` cho Command không trả dữ liệu.

#### 2. Biểu Diễn OOP, SOLID & Clean Architecture
* **Inheritance / Template:** Kế thừa `ApiController` để dùng chung `Sender` và `HandlerFailure` — không copy lại switch mã lỗi → HTTP status.
* **SRP:** Controller chỉ dịch HTTP ↔ Use Case. Quyền sở hữu (`itinerary.UserId == currentUserId`), rule publish (phải có ≥ 1 waypoint), re-index `order_index`... **đã nằm ở Handler extension `GetOwnedItineraryAsync` và Domain Entity** — Controller không kiểm tra lại.
* **DIP:** Controller chỉ phụ thuộc `ISender` (abstraction của MediatR), không biết Handler cụ thể, Repository hay `DbContext`.
* **Thin Controller ⇒ dễ test:** Toàn bộ nghiệp vụ test được ở tầng Application/Domain mà không cần dựng HTTP pipeline.

#### 3. Định Hướng & Tiêu Chuẩn Tự Code
* **Attributes cấp class:** `[Authorize]` (giống `ChatController`).
* **Route constraints:** Luôn dùng `{id:guid}`, `{waypointId:guid}`, `{dayNumber:int}`.
  > [!IMPORTANT]
  > `GET mine` và `GET {id:guid}` cùng cấp route. Nhờ constraint `:guid`, chuỗi `"mine"` không match `{id}` → không xung đột. **Bỏ constraint là bị 400/404 khó hiểu.**
* **Endpoint 1 – Create:** Trả **`201 Created`** qua `CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, ApiResponse<ItineraryDetailDto>.Success(...))` để Frontend nhận `Location` header và điều hướng sang editor.
* **Endpoint 2 – Mine:** Nhận `[FromQuery] int pageIndex = 1, int pageSize = 10, bool? isPublic = null`; giới hạn `[1, 100]` đã do `GetMyItinerariesQueryValidator` lo.
* **Endpoint 3 – GetById:** Gắn **`[AllowAnonymous]`** (giống `UsersController`). Lý do: `GetItineraryByIdQueryHandler` đã tự phân quyền — hành trình công khai ai cũng đọc được (phục vụ `ITINERARY_02_PUBLIC_VIEW`), bản nháp chỉ tác giả đọc được (trả `Itinerary.Forbidden` → `403`). Nếu để `[Authorize]`, khách sẽ bị chặn `401` trước khi vào Handler, phá vỡ luồng xem công khai.
* **Endpoint 5 – Publish:** `PUT {id:guid}/publish`, không body. **Đã chốt (Tech Lead):** giữ route `/publish` theo roadmap Phase 03.
* **Endpoint 10 – Reorder:** `dayNumber` lấy từ route, danh sách Id từ body → `new ReorderWaypointsCommand(id, dayNumber, request.OrderedWaypointIds)`.
* **`[ProducesResponseType]`:** Khai báo đủ `200/201`, `400`, `401`, `403`, `404` cho từng action để Swagger sinh tài liệu chính xác (theo mẫu `ChatController`).
* **`CancellationToken ct`:** Mọi action nhận và truyền xuống `Sender.Send(..., ct)` — client hủy request (đổi trang, kéo thả liên tục) thì query DB cũng dừng.
* **Log mẫu:** `_logger.LogInformation("Sắp xếp lại waypoint ngày {DayNumber} của hành trình {ItineraryId}", dayNumber, id);` — dùng placeholder, không nội suy chuỗi `$"..."`.

> [!NOTE]
> **Lỗi "Waypoint không tồn tại" đã được xử lý đúng:** `Itinerary.UpdateWaypoint` / `RemoveWaypoint` trả `Result.Failure(DomainErrors.Waypoint.NotFoundWithId(...))`, mã lỗi chứa `NotFound` nên `HandlerFailure` trả `404`. `WaypointNotFoundException` hiện không được ném ở đâu, và đã được đổi sang kế thừa `NotFoundException` để nếu sau này có ném thì `GlobalExceptionHandler` cũng trả `404`.

---

## PHẦN 3: KỊCH BẢN KIỂM THỬ THỦ CÔNG

---

### FILE 08: `Tripory.API.http` (Cập nhật)

#### 1. Mô hình 5W1H
* **Who (Ai):** Developer test nhanh bằng REST Client của VS Code / Visual Studio.
* **What (Là gì):** Bổ sung block request cho 11 endpoint, dùng biến `@token` (lấy từ `POST /api/v1/auth/login`) và `@itineraryId`, `@waypointId`.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.API/Tripory.API.http`.
* **Why (Tại sao):** Kịch bản test lưu cùng repo, tái chạy được sau mỗi lần sửa mà không cần thao tác Swagger lại từ đầu.

#### 2. Kịch Bản Test Theo Thứ Tự (Happy Path → Edge Cases)
1. **Login** lấy `accessToken` → gán `@token`.
2. **Create** `{"title": "  Hà Nội - Hà Giang  "}` → `201`, `title` đã được trim (VO `ItineraryTitle`), header `Location` trỏ về `/api/v1/itineraries/{id}`.
3. **Publish ngay** → `400 Itinerary.CannotPublishEmpty`.
4. **Add Waypoint** ngày 1 ×3 điểm, ngày 2 ×2 điểm → `order_index` lần lượt `0,1,2` và `0,1`.
5. **GetById** → `totalDistanceKm` = tổng cự ly ngày 1 + ngày 2, **không** cộng chặng qua đêm (điểm cuối ngày 1 → điểm đầu ngày 2).
6. **Reorder** ngày 1 đảo ngược thứ tự → `200`; GetById thấy thứ tự mới; gửi thiếu 1 Id → `400 Itinerary.InvalidReorderList`.
7. **Delete Waypoint** ở giữa ngày 1 → các điểm còn lại dồn về `0,1` liên tục.
8. **Set Subtitle** ngày 2 → `200`.
9. **Publish** → `200`.
10. **GetById không kèm token** → `200` (đã public). Tạo bản nháp khác rồi GetById không token → `403`.
11. **Mine** `?isPublic=true` → chỉ thấy hành trình đã xuất bản.
12. **Toạ độ sai** `longitude: 200` → `400` (FluentValidation hoặc `InvalidCoordinateException`).
13. **User B** thao tác `PUT` lên hành trình của User A → `403 Itinerary.Forbidden`.
14. **Delete Itinerary** → `200`; GetById lại → `404` (soft delete + global query filter).

---

## 🏁 Tiêu Chuẩn Nghiệm Thu & Kiểm Chứng (Quality Gate Bước 5)

1. **Biên dịch Solution:**
   ```powershell
   dotnet build
   ```
   **Kỳ vọng:** `Build succeeded` với `0 Warning(s), 0 Error(s)`.

2. **Chạy API & Swagger:**
   ```powershell
   docker compose up -d
   dotnet ef database update --project src/Services/Tripory/Tripory.Persistence --startup-project src/Services/Tripory/Tripory.API
   dotnet run --project src/Services/Tripory/Tripory.API
   ```
   **Kỳ vọng:** Swagger hiển thị nhóm `Itineraries` với đủ 11 endpoint, schema request/response đúng bảng hợp đồng.

3. **Checklist `AGENTS.md` cho từng action:**
   * [ ] Có `_logger.LogInformation(...)` ở dòng đầu.
   * [ ] Có `if (result.IsFailure) return HandlerFailure(result);` trước `Ok(...)`.
   * [ ] Không có `if` nghiệp vụ, không inject Repository/`DbContext`/`IGisDistanceCalculator`.
   * [ ] Không có `[HttpPatch]`.

4. **Kiểm tra Dependency Graph:** `Tripory.API` chỉ gọi Application qua `ISender`; Request contracts không bị Application tham chiếu ngược.

5. **Toàn bộ 14 kịch bản ở FILE 08 cho kết quả đúng kỳ vọng** — sẵn sàng bàn giao cho Frontend thay `mockItinerary.ts` bằng API thật.
