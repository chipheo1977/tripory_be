# HƯỚNG DẪN TỰ CODE BƯỚC 2: TẦNG APPLICATION (APPLICATION LAYER)
## Module: ITINERARY_01_MAP+TIMELINE (CQRS UseCases & Orchestration)

> **Dành cho:** Tech Lead / Developer tự tay triển khai code  
> **Vị trí lưu:** `C:\Users\OS\Documents\personal\tripory_be\GUIDE_STEP_02_APPLICATION_LAYER.md`  
> **Cấu trúc chuẩn:** Áp dụng $100\%$ template phân tích **TỪNG FILE** của `GUIDE_STEP_01_DOMAIN_LAYER.md` (kèm 5W1H, OOP, SOLID, DDD).  
> **Phong cách hướng dẫn:** Tập trung vào **Hợp đồng (Contracts), Ràng buộc (Invariants) và Luồng điều phối (Orchestration)**, tinh gọn tối đa các chỉ dẫn gõ code chi tiết để bạn hoàn toàn chủ động lập trình.

---

## 🗺️ Cây Thư Mục Các File Cần Triển Khai Trong Bước 2

```text
src/Services/Tripory/Tripory.Application/UseCases/V1/Itineraries/
├── Responses/
│   ├── WaypointDto.cs
│   ├── ItineraryDayDto.cs
│   ├── ItinerarySummaryDto.cs
│   └── ItineraryDetailDto.cs
│
├── Extensions/
│   └── ItineraryExtensions.cs
│
├── Commands/
│   ├── CreateQuickDraftItineraryCommand.cs
│   ├── CreateQuickDraftItineraryCommandHandler.cs
│   ├── CreateQuickDraftItineraryCommandValidator.cs
│   ├── UpdateItineraryMetadataCommand.cs
│   ├── UpdateItineraryMetadataCommandHandler.cs
│   ├── UpdateItineraryMetadataCommandValidator.cs
│   ├── PublishItineraryCommand.cs
│   ├── PublishItineraryCommandHandler.cs
│   ├── DeleteItineraryCommand.cs
│   ├── DeleteItineraryCommandHandler.cs
│   ├── AddWaypointCommand.cs
│   ├── AddWaypointCommandHandler.cs
│   ├── AddWaypointCommandValidator.cs
│   ├── UpdateWaypointCommand.cs
│   ├── UpdateWaypointCommandHandler.cs
│   ├── UpdateWaypointCommandValidator.cs
│   ├── DeleteWaypointCommand.cs
│   ├── DeleteWaypointCommandHandler.cs
│   ├── ReorderWaypointsCommand.cs
│   ├── ReorderWaypointsCommandHandler.cs
│   ├── ReorderWaypointsCommandValidator.cs
│   ├── SetDaySubtitleCommand.cs
│   ├── SetDaySubtitleCommandHandler.cs
│   └── SetDaySubtitleCommandValidator.cs
│
└── Queries/
    ├── GetItineraryByIdQuery.cs
    ├── GetItineraryByIdQueryHandler.cs
    ├── GetItineraryByIdQueryValidator.cs
    ├── GetMyItinerariesQuery.cs
    ├── GetMyItinerariesQueryHandler.cs
    └── GetMyItinerariesQueryValidator.cs
```

---

## PHẦN 1: RESPONSES (DTOs) & EXTENSIONS MAPPING

---

### FILE 1: `WaypointDto.cs`

#### 1. Mô hình 5W1H
* **Who:** API Controller nhận về từ Application Handler để trả về Client (UI Map & Timeline).
* **What:** Data Transfer Object (DTO) dạng `record` bất biến, chứa dữ liệu phẳng của một điểm dừng chân.
* **Where:** `Tripory.Application/UseCases/V1/Itineraries/Responses/WaypointDto.cs`.
* **When:** Được sinh ra khi truy vấn chi tiết hành trình hoặc sau khi thêm/sửa thành công một điểm dừng.
* **Why:** Tránh phơi bày trực tiếp Entity `Waypoint` ra ngoài HTTP API, bảo vệ cấu trúc nội bộ của Domain.
* **How:** Định nghĩa một C# `record` phẳng hóa tọa độ `Wgs84Coordinate` thành hai số thực `Longitude` và `Latitude`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong File Này
* **OOP & DDD:** Thể hiện tính **Trừu tượng hóa (Abstraction)** và **Bất biến (Immutability)**: DTO là một POCO thuần túy không chứa logic nghiệp vụ, bảo vệ biên giới giữa Domain và thế giới bên ngoài.
* **SOLID (SRP):** Chịu trách nhiệm duy nhất là định hình cấu trúc dữ liệu của `Waypoint` khi truyền qua mạng.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **Kiểu dữ liệu:** `public record WaypointDto(...)`.
* **Thuộc tính cần có:**
  * `Guid Id`, `int DayNumber`, `int OrderIndex`, `string Name`, `string? Address`, `double Longitude`, `double Latitude`, `string? Notes`.

---

### FILE 2: `ItineraryDayDto.cs`

#### 1. Mô hình 5W1H
* **Who:** Client Mobile/Web sử dụng để hiển thị các mốc ngày (Day Tabs / Subheaders).
* **What:** DTO chứa thông tin tóm tắt của một mốc ngày trong hành trình.
* **Where:** `Tripory.Application/UseCases/V1/Itineraries/Responses/ItineraryDayDto.cs`.
* **When:** Dùng trong màn hình chi tiết hành trình hoặc khi chuyển đổi tab ngày trên Day Switcher.
* **Why:** Giúp Client nắm bắt được phụ đề của ngày (`Subtitle`) và tổng cự ly tích lũy của riêng ngày đó (`DayDistanceKm`).
* **How:** Khai báo `record` chứa các thuộc tính mốc ngày tương ứng với Entity `ItineraryDay`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong File Này
* **DDD:** Phản ánh đúng cấu trúc phân cấp ngày của Aggregate `Itinerary`.
* **SOLID (SRP):** Chỉ đại diện cho dữ liệu hiển thị của mốc ngày du lịch.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **Kiểu dữ liệu:** `public record ItineraryDayDto(...)`.
* **Thuộc tính cần có:**
  * `Guid Id`, `int DayNumber`, `string? Subtitle`, `double DayDistanceKm`.

---

### FILE 3: `ItinerarySummaryDto.cs`

#### 1. Mô hình 5W1H
* **Who:** Màn hình danh sách ("Chuyến đi của tôi", Feed khám phá).
* **What:** DTO tóm tắt tối ưu kích thước, không kèm danh sách chi tiết các điểm dừng để tiết kiệm băng thông mạng.
* **Where:** `Tripory.Application/UseCases/V1/Itineraries/Responses/ItinerarySummaryDto.cs`.
* **When:** Dùng trong các câu query phân trang danh sách chuyến đi (`GetMyItinerariesQuery`).
* **Why:** Tối ưu hóa hiệu năng truyền tải dữ liệu qua mạng; màn hình danh sách chỉ cần số lượng điểm và số ngày, không cần tọa độ chi tiết.
* **How:** Thay vì nhúng cả list con, chỉ lưu `DaysCount` và `WaypointsCount`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong File Này
* **SOLID (ISP):** Phân tách giao diện dữ liệu: Client xem danh sách không bị ép tải về dữ liệu chi tiết của từng waypoint.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **Kiểu dữ liệu:** `public record ItinerarySummaryDto(...)`.
* **Thuộc tính cần có:**
  * `Guid Id`, `Guid UserId`, `string Title`, `string? Description`, `string? CoverImageUrl`, `DateOnly? StartDate`, `bool IsPublic`, `double TotalDistanceKm`, `int DaysCount`, `int WaypointsCount`, `DateTimeOffset CreatedAt`.

---

### FILE 4: `ItineraryDetailDto.cs`

#### 1. Mô hình 5W1H
* **Who:** Màn hình biên tập Editor và màn hình xem chi tiết hành trình toàn cảnh (Map + Timeline).
* **What:** DTO đầy đủ nhất đại diện cho toàn bộ Aggregate `Itinerary`, bao gồm thông tin chuyến đi và toàn bộ danh sách con `Days` và `Waypoints`.
* **Where:** `Tripory.Application/UseCases/V1/Itineraries/Responses/ItineraryDetailDto.cs`.
* **When:** Trả về khi người dùng mở Editor hoặc xem chi tiết một hành trình cụ thể qua `GetItineraryByIdQuery`.
* **Why:** Cung cấp đầy đủ tọa độ và thứ tự các điểm dừng để Mapbox/Leaflet vẽ đường nối và Timeline dựng thẻ.
* **How:** Chứa các trường metadata của `Itinerary` kèm `IReadOnlyList<ItineraryDayDto> Days` và `IReadOnlyList<WaypointDto> Waypoints`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong File Này
* **DDD:** Tái hiện đầy đủ ranh giới của Aggregate Root và các Entity con khi vận chuyển ra tầng Client.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **Kiểu dữ liệu:** `public record ItineraryDetailDto(...)`.
* **Thuộc tính cần có:**
  * Metadata chuyến đi (`Id`, `UserId`, `Title`, `Description`, `CoverImageUrl`, `StartDate`, `IsPublic`, `TotalDistanceKm`, `CreatedAt`, `UpdatedAt`).
  * `IReadOnlyList<ItineraryDayDto> Days`.
  * `IReadOnlyList<WaypointDto> Waypoints`.

---

### FILE 5: `ItineraryExtensions.cs`

#### 1. Mô hình 5W1H
* **Who:** Tầng Application Handlers sử dụng để chuyển đổi nhanh Entity sang DTO.
* **What:** Static Class chứa các Extension Methods phục vụ ánh xạ dữ liệu thuần túy (Pure Mapping Functions).
* **Where:** `Tripory.Application/UseCases/V1/Itineraries/Extensions/ItineraryExtensions.cs`.
* **When:** Được gọi ở bước cuối cùng của mỗi Handler trước khi trả về `Result<T>`.
* **Why:** Tuân thủ **Pattern 1 trong `AGENTS.md`**: loại bỏ hoàn toàn việc gõ code `new Dto(...)` lặp lại nhiều nơi, tăng tính tái sử dụng và khả năng bảo trì.
* **How:** Nhận `this Waypoint`, `this ItineraryDay`, `this Itinerary` và trả về DTO tương ứng.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong File Này
* **SOLID (SRP):** Chịu trách nhiệm duy nhất là chuyển đổi hình dạng dữ liệu (Data Transformation) từ Domain Model sang Representation Model.
* **OOP (Polymorphism & Extensibility):** Mở rộng tính năng ánh xạ cho Entity mà không cần sửa đổi mã nguồn nội bộ của Entity.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **Namespace:** `Tripory.Application.UseCases.V1.Itineraries.Extensions`.
* **Các phương thức cần viết:**
  * `public static WaypointDto ToDto(this Waypoint waypoint)`: Lấy `waypoint.Coordinate.Longitude`, `Latitude`, `Name.Value`.
  * `public static ItineraryDayDto ToDto(this ItineraryDay day)`.
  * `public static ItinerarySummaryDto ToSummaryDto(this Itinerary itinerary)`: Tính `DaysCount = itinerary.Days.Count`, `WaypointsCount = itinerary.Waypoints.Count`.
  * `public static ItineraryDetailDto ToDetailDto(this Itinerary itinerary)`: Map danh sách `Days` và `Waypoints` sang danh sách DTO con.

---

## PHẦN 2: COMMANDS (VÒNG ĐỜI & METADATA HÀNH TRÌNH)

---

### FILE 6, 7, 8: `CreateQuickDraftItinerary` (Tạo Nhanh Bản Nháp)

#### 1. Mô hình 5W1H
* **Who:** Người dùng vừa đăng nhập bấm nút "+ Tạo lịch trình mới".
* **What:** Use Case khởi tạo nhanh một chuyến đi dạng bản nháp với chỉ duy nhất Tiêu đề chuyến đi (`Title`), tự động sinh sẵn Ngày 1.
* **Where:**
  * File 6: `CreateQuickDraftItineraryCommand.cs`
  * File 7: `CreateQuickDraftItineraryCommandHandler.cs`
  * File 8: `CreateQuickDraftItineraryCommandValidator.cs`
* **When:** Giai đoạn 1 của luồng nhập liệu UX theo `PT-01_metadata.md`.
* **Why:** Giảm thiểu rào cản thao tác, đưa người dùng ngay lập tức vào màn hình lập lịch trình.
* **How:** Khởi tạo Command $\rightarrow$ Pipeline Validate $\rightarrow$ Handler lấy `UserId`, tạo `ItineraryTitle`, gọi `Itinerary.CreateQuickDraft`, lưu CSDL và trả về `ItineraryDetailDto`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **CQRS & ISP:** Command chỉ chứa đúng trường `Title` cần thiết cho tác vụ tạo nhanh.
* **DIP:** Handler phụ thuộc vào `IItineraryRepository`, `IUnitOfWork`, `ICurrentUserService`.
* **DDD:** Aggregate Root `Itinerary` tự quản lý việc sinh sẵn Ngày 1 bên trong nó.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 6 (`Command`):**
  * `public record CreateQuickDraftItineraryCommand(string Title) : ICommand<ItineraryDetailDto>;`
* **File 8 (`Validator`):**
  * Kế thừa `AbstractValidator<CreateQuickDraftItineraryCommand>`.
  * Rule: `Title` không rỗng, độ dài từ 1 đến 100 ký tự.
* **File 7 (`Handler`):**
  * Kế thừa `ICommandHandler<CreateQuickDraftItineraryCommand, ItineraryDetailDto>`.
  * **Orchestration Flow:**
    1. Kiểm tra `_currentUserService.UserId`. Nếu không có $\rightarrow$ trả lỗi `Auth.Unauthorized`.
    2. Gọi factory method `ItineraryTitle.Create(request.Title)`. Nếu lỗi $\rightarrow$ trả về Failure.
    3. Gọi `Itinerary.CreateQuickDraft(currentUserId, titleVo.Value)`.
    4. Gọi `_itineraryRepository.AddAsync(...)` $\rightarrow$ `_unitOfWork.SaveChangesAsync(...)`.
    5. Trả về `Result.Success(itinerary.ToDetailDto())`.

---

### FILE 9, 10, 11: `UpdateItineraryMetadata` (Cập Nhật Thông Tin Chuyến Đi)

#### 1. Mô hình 5W1H
* **Who:** Tác giả chuyến đi hoàn thiện thông tin trước khi chia sẻ.
* **What:** Use Case cập nhật Tiêu đề, Mô tả, Ngày khởi hành và URL Ảnh bìa.
* **Where:** `UpdateItineraryMetadataCommand.cs`, `...Handler.cs`, `...Validator.cs`.
* **When:** Giai đoạn 2 (Pre-publish settings) theo `PT-01_metadata.md`.
* **Why:** Cho phép điều chỉnh thông tin tổng quan của hành trình bất kỳ lúc nào.
* **How:** Handler kiểm tra quyền sở hữu $\rightarrow$ tạo Value Objects $\rightarrow$ gọi `itinerary.UpdateMetadata(...)`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **Bảo Mật Quyền Sở Hữu (Data Ownership):** Handler bảo vệ tài nguyên bằng cách so sánh `itinerary.UserId == currentUserId`.
* **DDD:** Chuyển đổi dữ liệu nguyên thủy từ Command thành các Value Objects (`ItineraryTitle`, `ItineraryDescription`) trước khi truyền xuống Domain.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 9 (`Command`):**
  * `public record UpdateItineraryMetadataCommand(Guid ItineraryId, string Title, string? Description, DateOnly? StartDate, string? CoverImageUrl) : ICommand;`
* **File 11 (`Validator`):**
  * `ItineraryId` không được rỗng; `Title` từ 1 đến 100 ký tự; `Description` tối đa 1000 ký tự (nếu có).
* **File 10 (`Handler`):**
  * **Orchestration Flow:**
    1. Lấy `currentUserId`.
    2. Đọc `itinerary` từ `_itineraryRepository.GetByIdAsync(...)`. Nếu null $\rightarrow$ trả lỗi 404 `"Itinerary.NotFound"`.
    3. **Resource Auth:** `if (itinerary.UserId != currentUserId)` $\rightarrow$ trả lỗi 403 `"Itinerary.Forbidden"`.
    4. Khởi tạo `ItineraryTitle.Create(...)` và `ItineraryDescription.Create(...)`.
    5. Gọi Domain method: `itinerary.UpdateMetadata(titleVo.Value, descVo.Value, request.StartDate, request.CoverImageUrl)`.
    6. `_itineraryRepository.UpdateAsync(...)` $\rightarrow$ `_unitOfWork.SaveChangesAsync(...)`.
    7. Trả về `Result.Success()`.

---

### FILE 12, 13: `PublishItinerary` (Xuất Bản Hành Trình)

#### 1. Mô hình 5W1H
* **Who:** Người tạo lịch trình bấm nút [Xuất bản].
* **What:** Chuyển trạng thái hành trình từ Bản nháp (`is_public = false`) sang Công khai (`is_public = true`).
* **Where:** `PublishItineraryCommand.cs`, `PublishItineraryCommandHandler.cs`.
* **When:** Khi người dùng muốn đưa chuyến đi lên cộng đồng Discovery Feed.
* **Why:** Thực hiện bước chuyển trạng thái (State Transition) quan trọng nhất của vòng đời chuyến đi.
* **How:** Handler gọi method `itinerary.Publish()`. Domain tự động chặn xuất bản nếu chưa có điểm dừng chân nào (`BR_05`).

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **Pattern 2 trong `AGENTS.md`:** Handler tuyệt đối không tự viết `if (waypoints.Count == 0) throw...`, mà gọi trực tiếp xuống Domain Entity để Entity tự bảo vệ invariant xuất bản.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 12 (`Command`):** `public record PublishItineraryCommand(Guid ItineraryId) : ICommand;`
* **File 13 (`Handler`):**
  * Flow: Đọc Entity $\rightarrow$ Kiểm tra quyền sở hữu của `currentUserId` $\rightarrow$ Gọi `var result = itinerary.Publish();`.
  * Nếu `result.IsFailure` $\rightarrow$ trả ngay lỗi của Domain.
  * Nếu thành công: Cập nhật DB và trả về `Result.Success()`.

---

### FILE 14, 15: `DeleteItinerary` (Xóa Mềm Hành Trình)

#### 1. Mô hình 5W1H
* **Who:** Tác giả chuyến đi muốn gỡ bỏ một hành trình.
* **What:** Use Case đánh dấu xóa mềm (`Soft Delete`) hành trình.
* **Where:** `DeleteItineraryCommand.cs`, `DeleteItineraryCommandHandler.cs`.
* **When:** Người dùng bấm nút xóa chuyến đi trong danh sách cá nhân.
* **Why:** Tuân thủ BRD 4.3 về Soft Delete: không xóa vật lý bản ghi trong DB để bảo toàn toàn vẹn dữ liệu.
* **How:** Gọi `itinerary.MarkAsDeleted(currentUserId)` được thừa hưởng từ `EntityFullAuditBase`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **LSP (Liskov Substitution):** Tận dụng trực tiếp method `MarkAsDeleted()` của lớp cơ sở `EntityFullAuditBase`.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 14 (`Command`):** `public record DeleteItineraryCommand(Guid ItineraryId) : ICommand;`
* **File 15 (`Handler`):**
  * Flow: Đọc Entity $\rightarrow$ Kiểm tra quyền sở hữu $\rightarrow$ Gọi `itinerary.MarkAsDeleted(currentUserId)` $\rightarrow$ Lưu DB $\rightarrow$ Trả về `Result.Success()`.

---

## PHẦN 3: COMMANDS (ĐIỂM DỪNG WAYPOINTS & QUY HOẠCH NGÀY)

---

### FILE 16, 17, 18: `AddWaypoint` (Thêm Điểm Dừng Chân)

#### 1. Mô hình 5W1H
* **Who:** Người dùng chọn kết quả từ thanh tìm kiếm Geocoding (`PT-02_waypoint search.md`).
* **What:** Thêm một điểm dừng chân vào một ngày cụ thể, tự gán thứ tự cuối ngày và tính toán lại cự ly toàn chuyến.
* **Where:** `AddWaypointCommand.cs`, `AddWaypointCommandHandler.cs`, `AddWaypointCommandValidator.cs`.
* **When:** Người dùng bấm "Thêm vào Ngày X".
* **Why:** Tạo mới thực thể con trong Aggregate và cập nhật tức thì cự ly trắc địa GIS.
* **How:** Handler phối hợp giữa Aggregate Root `Itinerary` và Port `IGisDistanceCalculator`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **DDD Aggregate Pattern:** Điểm dừng chân không được thêm trực tiếp qua `WaypointRepository`, mà bắt buộc thêm thông qua Aggregate Root `itinerary.AddWaypoint(...)`.
* **DIP:** Inject `IGisDistanceCalculator` vào Handler để truyền vào `itinerary.RecalculateDistances(calculator)`.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 16 (`Command`):**
  * `public record AddWaypointCommand(Guid ItineraryId, int DayNumber, string Name, string? Address, double Longitude, double Latitude, string? Notes) : ICommand<WaypointDto>;`
* **File 18 (`Validator`):**
  * `ItineraryId` không rỗng; `DayNumber >= 1`; `Name` không rỗng, $\le 200$ ký tự; `Longitude` $[-180, 180]$; `Latitude` $[-90, 90]$.
* **File 17 (`Handler`):**
  * **Dependencies cần inject:** `IItineraryRepository`, `IUnitOfWork`, `ICurrentUserService`, `IGisDistanceCalculator`.
  * **Orchestration Flow:**
    1. Lấy `currentUserId`.
    2. Đọc `itinerary` (sử dụng method có eager load các bảng con, ví dụ: `GetByIdWithDetailsAsync`).
    3. Kiểm tra quyền sở hữu (`itinerary.UserId == currentUserId`).
    4. Tạo VOs: `WaypointName.Create(...)`, `Wgs84Coordinate.Create(...)`.
    5. Gọi Domain: `var addResult = itinerary.AddWaypoint(request.DayNumber, nameVo.Value, request.Address, coordVo.Value, request.Notes)`.
    6. **Tính cự ly:** Gọi `itinerary.RecalculateDistances(_gisCalculator)`.
    7. Lưu DB và trả về `Result.Success(addResult.Value.ToDto())`.

---

### FILE 19, 20, 21: `UpdateWaypoint` (Chỉnh Sửa Điểm Dừng Chân)

#### 1. Mô hình 5W1H
* **Who:** Người dùng bấm vào thẻ điểm dừng và cập nhật thông tin/tọa độ.
* **What:** Cập nhật Tên, Địa chỉ, Tọa độ hoặc Ghi chú cá nhân của một waypoint.
* **Where:** `UpdateWaypointCommand.cs`, `...Handler.cs`, `...Validator.cs`.
* **When:** Khi người dùng muốn đổi tên điểm tham quan hoặc sửa ghi chú.
* **Why:** Đảm bảo điểm dừng luôn có thông tin chính xác nhất.
* **How:** Handler gọi method `itinerary.UpdateWaypoint(...)` và tính lại cự ly.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **Encapsulation:** Cập nhật thông qua Aggregate Root, đảm bảo `UpdatedAt` của chuyến đi và điểm dừng được đồng bộ.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 19 (`Command`):**
  * `public record UpdateWaypointCommand(Guid ItineraryId, Guid WaypointId, string Name, string? Address, double Longitude, double Latitude, string? Notes) : ICommand;`
* **File 21 (`Validator`):** Tương tự `AddWaypoint`, bổ sung kiểm tra `WaypointId` không rỗng.
* **File 20 (`Handler`):**
  * Flow: Đọc Entity chi tiết $\rightarrow$ Kiểm tra quyền sở hữu $\rightarrow$ Tạo VOs $\rightarrow$ Gọi `itinerary.UpdateWaypoint(...)` $\rightarrow$ Gọi `itinerary.RecalculateDistances(_gisCalculator)` $\rightarrow$ Lưu DB $\rightarrow$ Trả về `Result.Success()`.

---

### FILE 22, 23: `DeleteWaypoint` (Xóa Điểm Dừng Chân)

#### 1. Mô hình 5W1H
* **Who:** Người dùng bấm icon thùng rác trên thẻ điểm dừng (`PT-03_waypoint list.md`).
* **What:** Xóa một điểm dừng chân khỏi hành trình.
* **Where:** `DeleteWaypointCommand.cs`, `DeleteWaypointCommandHandler.cs`.
* **When:** Thao tác xóa điểm dừng trên Timeline.
* **Why:** Kích hoạt cơ chế chuẩn hóa lại chỉ mục thứ tự `order_index` liên tục `0, 1, 2...` và trừ cự ly ngày tương ứng.
* **How:** Gọi `itinerary.RemoveWaypoint(waypointId)` và `itinerary.RecalculateDistances(...)`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **Bảo Toàn Invariant (DDD):** Aggregate Root tự động đánh số lại các điểm còn lại trong ngày theo `BR_04`, Handler không phải can thiệp thủ công.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 22 (`Command`):** `public record DeleteWaypointCommand(Guid ItineraryId, Guid WaypointId) : ICommand;`
* **File 23 (`Handler`):**
  * Flow: Đọc Entity chi tiết $\rightarrow$ Kiểm tra quyền $\rightarrow$ Gọi `itinerary.RemoveWaypoint(request.WaypointId)` $\rightarrow$ Gọi `itinerary.RecalculateDistances(_gisCalculator)` $\rightarrow$ Lưu DB $\rightarrow$ Trả về `Result.Success()`.

---

### FILE 24, 25, 26: `ReorderWaypoints` (Kéo Thả Sắp Xếp Thứ Tự)

#### 1. Mô hình 5W1H
* **Who:** Người dùng thực hiện thao tác kéo thả thẻ trong ngày (`PR-04_waypoint order.md`).
* **What:** Nhận danh sách các `Guid WaypointId` theo thứ tự mới của một ngày và cập nhật lại `OrderIndex`.
* **Where:** `ReorderWaypointsCommand.cs`, `...Handler.cs`, `...Validator.cs`.
* **When:** Kích hoạt duy nhất 1 lần khi người dùng thả thẻ (`onDragEnd`).
* **Why:** Tối ưu hóa hiệu năng, cập nhật hàng loạt thứ tự và vẽ lại đường nối bản đồ tức thì.
* **How:** Gọi `itinerary.ReorderWaypointsInDay(...)` và tính lại cự ly.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **Tính toàn vẹn (Integrity Check):** Domain tự kiểm tra danh sách ID gửi lên có trùng khớp $100\%$ với các điểm đang có trong ngày hay không.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 24 (`Command`):**
  * `public record ReorderWaypointsCommand(Guid ItineraryId, int DayNumber, IReadOnlyList<Guid> OrderedWaypointIds) : ICommand;`
* **File 26 (`Validator`):** `DayNumber >= 1`; `OrderedWaypointIds` không được rỗng.
* **File 25 (`Handler`):**
  * Flow: Đọc Entity chi tiết $\rightarrow$ Kiểm tra quyền $\rightarrow$ Gọi `itinerary.ReorderWaypointsInDay(request.DayNumber, request.OrderedWaypointIds)` $\rightarrow$ Gọi `itinerary.RecalculateDistances(_gisCalculator)` $\rightarrow$ Lưu DB $\rightarrow$ Trả về `Result.Success()`.

---

### FILE 27, 28, 29: `SetDaySubtitle` (Đặt Tiêu Đề Phụ Cho Ngày)

#### 1. Mô hình 5W1H
* **Who:** Người dùng nhập phụ đề cho ngày (ví dụ: "Ngày 1 - Chinh phục Đồng Văn").
* **What:** Cập nhật trường `Subtitle` của thực thể `ItineraryDay`.
* **Where:** `SetDaySubtitleCommand.cs`, `...Handler.cs`, `...Validator.cs`.
* **When:** Người dùng chỉnh sửa tiêu đề trên thanh Day Switcher.
* **How:** Gọi method `itinerary.SetDaySubtitle(dayNumber, subtitle)`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **Encapsulation:** Phụ đề ngày được quản lý thông qua Aggregate Root, tự động sinh ngày nếu mốc ngày chưa có.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 27 (`Command`):** `public record SetDaySubtitleCommand(Guid ItineraryId, int DayNumber, string? Subtitle) : ICommand;`
* **File 29 (`Validator`):** `DayNumber >= 1`; nếu có `Subtitle` thì không vượt quá 100 ký tự.
* **File 28 (`Handler`):**
  * Flow: Đọc Entity $\rightarrow$ Kiểm tra quyền $\rightarrow$ Gọi `itinerary.SetDaySubtitle(...)` $\rightarrow$ Lưu DB $\rightarrow$ Trả về `Result.Success()`.

---

## PHẦN 4: QUERIES (TRUY VẤN DỮ LIỆU)

---

### FILE 30, 31, 32: `GetItineraryById` (Lấy Chi Tiết Hành Trình)

#### 1. Mô hình 5W1H
* **Who:** Bất kỳ người dùng nào mở xem hành trình (hoặc chính tác giả mở Editor).
* **What:** Query truy vấn đầy đủ cấu trúc chuyến đi kèm danh sách ngày và điểm dừng.
* **Where:** `GetItineraryByIdQuery.cs`, `...Handler.cs`, `...Validator.cs`.
* **When:** Mở trang `/itineraries/{id}`.
* **Why:** Cung cấp nguồn dữ liệu toàn diện cho bản đồ tương tác và dòng thời gian.
* **How:** Đọc dữ liệu từ Repository $\rightarrow$ Áp dụng quy tắc phân quyền truy cập dữ liệu (Public vs Private) $\rightarrow$ Map sang `ItineraryDetailDto`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **Bảo Mật Truy Cập (Data Access Policy):**
  * Nếu chuyến đi công khai (`IsPublic == true`): Bất kỳ ai cũng có quyền đọc.
  * Nếu chuyến đi bản nháp (`IsPublic == false`): Chỉ DUY NHẤT tác giả (`UserId == currentUserId`) mới được phép đọc.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 30 (`Query`):** `public record GetItineraryByIdQuery(Guid ItineraryId) : IQuery<ItineraryDetailDto>;`
* **File 32 (`Validator`):** `ItineraryId` không được rỗng.
* **File 31 (`Handler`):**
  * **Orchestration Flow:**
    1. Đọc `itinerary` qua `_itineraryRepository.GetByIdWithDetailsAsync(request.ItineraryId, ct)`. Nếu null $\rightarrow$ trả lỗi 404 `"Itinerary.NotFound"`.
    2. Nếu `!itinerary.IsPublic`:
       * Kiểm tra xem người gọi có đăng nhập không và có phải chính chủ không (`_currentUserService.UserId != itinerary.UserId`).
       * Nếu không phải chính chủ $\rightarrow$ trả lỗi 403 `"Itinerary.Forbidden"`.
    3. Trả về `Result.Success(itinerary.ToDetailDto())`.

---

### FILE 33, 34, 35: `GetMyItineraries` (Lấy Danh Sách Hành Trình Cá Nhân)

#### 1. Mô hình 5W1H
* **Who:** Người dùng đã đăng nhập vào xem tab "Chuyến đi của tôi".
* **What:** Query phân trang danh sách các hành trình do chính người dùng đó tạo ra, hỗ trợ lọc theo trạng thái công khai/bản nháp.
* **Where:** `GetMyItinerariesQuery.cs`, `...Handler.cs`, `...Validator.cs`.
* **When:** Người dùng truy cập trang Dashboard cá nhân.
* **Why:** Hiển thị danh sách hành trình gọn nhẹ, hỗ trợ phân trang chuẩn `PagedResult<T>`.
* **How:** Gọi `GetMyItinerariesPagedAsync` từ Repository $\rightarrow$ Map danh sách sang `ItinerarySummaryDto`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong Nhóm File Này
* **ISP & CQRS:** Tối ưu hóa truy vấn: không nạp chi tiết toàn bộ waypoints lên bộ nhớ mà chỉ lấy thông tin tóm tắt và số lượng.

#### 3. Định Hướng & Tiêu Chuẩn Để Bạn Tự Code
* **File 33 (`Query`):**
  * `public record GetMyItinerariesQuery(int PageIndex = 1, int PageSize = 10, bool? IsPublic = null) : IQuery<PagedResult<ItinerarySummaryDto>>;`
* **File 35 (`Validator`):** `PageIndex >= 1`; `PageSize` trong khoảng $[1, 100]$.
* **File 34 (`Handler`):**
  * **Orchestration Flow:**
    1. Kiểm tra đăng nhập qua `_currentUserService.UserId`.
    2. Gọi `_itineraryRepository.GetMyItinerariesPagedAsync(currentUserId, request.PageIndex, request.PageSize, request.IsPublic, ct)`.
    3. Map kết quả sang danh sách `ItinerarySummaryDto` qua `ToSummaryDto()`.
    4. Trả về `Result.Success(new PagedResult<ItinerarySummaryDto>(items, totalCount, pageIndex, pageSize))`.

---

## 🎯 Tiêu Chuẩn Nghiệm Thu & Kiểm Chứng (Quality Gate)

Sau khi bạn hoàn thành việc viết các file trên:
1. **Kiểm tra biên dịch:**
   ```powershell
   dotnet build src/Services/Tripory/Tripory.Application/Tripory.Application.csproj
   ```
   **Kỳ vọng:** `Build succeeded` với `0 Warning(s), 0 Error(s)`.
2. **Quy tắc phân tầng:** Tầng Application **tuyệt đối không tham chiếu EF Core (`Microsoft.EntityFrameworkCore`)** hay trực tiếp inject `DbContext`. Toàn bộ dữ liệu đi qua `IItineraryRepository` và `IUnitOfWork`.
3. **Quy tắc bảo mật:** Tất cả các lệnh chỉnh sửa/xóa/đọc bản nháp đều được bảo vệ bởi điều kiện `itinerary.UserId == currentUserId`.
