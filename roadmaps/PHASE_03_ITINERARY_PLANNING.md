# PHASE_03_ITINERARY_PLANNING.md – Kế Hoạch Triển Khai Giai Đoạn 3: Lập Kế Hoạch & Trực Quan Hóa Lịch Trình (ITINERARY_01)

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 03 – Core Itinerary Planning Engine (Map + Timeline)  
> **Lựa chọn kiến trúc:** Clean Architecture + DDD + CQRS + NetTopologySuite PostGIS (Kế thừa tiêu chuẩn từ `tc-ems-be` và ràng buộc trong `AGENTS.md`)  
> **Nguồn đặc tả nghiệp vụ:** `../tripory/docs/traveler/ITINERARY_01_MAP+TIMELINE` (`create map+timeline.md`, `PT-01` đến `PT-05`) & `BRD_Travel_Itinerary_App.md` (Sprint 3.1)

---

## 🎯 1. Mục Tiêu & Phạm Vi (Scope)

* **Mục tiêu:** Xây dựng Core Domain Aggregate trung tâm của nền tảng Tripory: Cho phép người dùng tạo, quản lý lịch trình du lịch đa ngày, ghim điểm dừng chân (Waypoints) với tọa độ địa lý WGS84, sắp xếp thứ tự (Reorder), tính toán cự ly trắc địa GIS chuẩn xác và quản lý vòng đời Xuất bản (`Draft` $\rightarrow$ `Published`).
* **Phạm vi triển khai (In-Scope):**
  * **Aggregate Root `Itinerary`:** Quản lý thông tin chuyến đi (Tiêu đề $1-100$ ký tự, Mô tả $\le 1000$ ký tự, Ngày khởi hành ISO-8601, Ảnh bìa $\le 10\text{MB}$, Trạng thái công khai `is_public`).
  * **Thực thể Phân cấp Ngày & Điểm dừng (`ItineraryDay` & `Waypoint`):**
    * Quản lý chuyến đi theo ngày (`day_number` $\ge 1$, phụ đề `subtitle`).
    * Điểm dừng chân mang tọa độ PostGIS `Point` SRID 4326 (WGS84 `[lng, lat]`), tên địa điểm, địa chỉ chuẩn hóa, ghi chú.
    * Đánh số thứ tự trong ngày liên tục 0-indexed (`order_index: 0, 1, 2...`).
  * **Động cơ Sắp xếp & Chuẩn hóa Thứ tự (Reorder Engine):**
    * API Batch Reorder các điểm trong ngày, tự động bảo toàn chuỗi liên tục, không ngắt quãng.
    * Tự động chuẩn hóa lại chỉ mục khi xóa điểm dừng chân.
  * **Động cơ Tính toán Trắc địa GIS (Geodesic / Haversine Engine):**
    * Tích hợp `NetTopologySuite` PostGIS tính khoảng cách đường chim bay giữa các điểm trong ngày.
    * Quy tắc ngắt quãng qua đêm: Tuyệt đối không cộng cự ly giữa điểm cuối Ngày $N$ và điểm đầu Ngày $N+1$ vào cự ly ngày.
    * Tính tổng cự ly từng ngày và tổng cự ly toàn chuyến đi (`total_distance_km`).
  * **Upload & Xử lý Ảnh Bìa Chuyến Đi (Cover Image $\le 10\text{MB}$):**
    * Tải file ảnh qua API multipart, kiểm tra dung lượng $\le 10\text{MB}$, định dạng JPEG/PNG/WEBP, lưu trữ cục bộ/MinIO và phát hành URL.
  * **Đồng bộ & Dữ liệu mẫu (Demo Itinerary Seeder):**
    * Endpoint cấp dữ liệu mẫu chuẩn (Hà Nội - Hà Giang 3 ngày) hỗ trợ kiểm thử UI/UX Frontend.
* **Phạm vi dành cho các Phase tiếp theo (Deferred / Out-of-Scope):**
  * ⏸️ **Directions API (Đường bộ thực tế):** Tích hợp OSRM / Mapbox Directions cho tuyến đường xe chạy sẽ đưa vào Milestone mở rộng sau.
  * ⏸️ **Fork & Lineage Tracking:** Thuộc phân hệ `ITINERARY_02_PUBLIC_VIEW` (Phase 04).
  * ⏸️ **Discovery Feed & Social Engagement (Like/Comment):** Thuộc phân hệ `DISCOVERY_FEED_01` (Phase 05).

---

## 📊 2. Bảng Tiến Độ Triển Khai 4 Milestones

```
[Milestone 3.1: Itinerary Core Aggregate & Quick Draft]
                       │
                       ▼
[Milestone 3.2: Waypoint Management, PostGIS & Haversine Distance]
                       │
                       ▼
[Milestone 3.3: Drag-Drop Reorder Engine & Day Schedule]
                       │
                       ▼
[Milestone 3.4: Cover Image Upload & Publish Workflow]
```

| Milestone | Nội dung trọng tâm | Trạng thái | Ghi chú |
| :---: | :--- | :---: | :--- |
| **3.1** | **Itinerary Core Aggregate & Quick Draft** | `[ ] Chưa bắt đầu` | Khởi tạo nhanh bản nháp, CRUD Metadata, List/Detail |
| **3.2** | **Waypoints, PostGIS Geometry & Geodesic Distance** | `[ ] Chưa bắt đầu` | Thêm/Sửa/Xóa Waypoint, WGS84 Point, Haversine GIS calculator |
| **3.3** | **Batch Reorder Engine & Day Management** | `[ ] Chưa bắt đầu` | Kéo thả thứ tự `order_index`, re-index khi xóa, Day Subtitles |
| **3.4** | **Cover Image Upload & Publish Workflow** | `[ ] Chưa bắt đầu` | Upload ảnh bìa $\le 10\text{MB}$, Pre-publish validation, Demo seed |

---

## 🏗️ 3. Quy Trình 6 Bước Triển Khai "Từ Trong Ra Ngoài" (Inside-Out)

```
[Bước 1: Domain Layer (Itinerary, Waypoint, VOs & Invariants)]
              │
              ▼
[Bước 2: Application Layer (CQRS UseCases, Validators & GIS Port)]
              │
              ▼
[Bước 3: Persistence Layer (EF Core NetTopologySuite, PostGIS & Repo)]
              │
              ▼
[Bước 4: Infrastructure Layer (GIS Calculator & Cover Image Storage)]
              │
              ▼
[Bước 5: API Layer (ItinerariesController REST Endpoints)]
              │
              ▼
[Bước 6: Verification & Database Migration Testing]
```

---

### BƯỚC 1: Tầng `Tripory.Domain` (Core Aggregate - Pure C#)
* **Trách nhiệm:** Bảo vệ toàn bộ invariants nghiệp vụ chuyến đi, tọa độ địa lý, giới hạn ký tự và quy tắc chuyển trạng thái.
* **Entities & Value Objects:**
  * `Itinerary` (Aggregate Root):
    * `Id` (Guid), `UserId` (Guid), `Title` (ItineraryTitle VO), `Description` (string?, $\le 1000$ ký tự), `CoverImageUrl` (string?), `StartDate` (DateOnly?), `IsPublic` (bool), `TotalDistanceKm` (double), `CreatedAt`, `UpdatedAt`.
    * Collections: `_days` (`List<ItineraryDay>`), `_waypoints` (`List<Waypoint>`).
    * Domain Methods:
      * `CreateQuickDraft(userId, title)`
      * `UpdateMetadata(title, description, startDate, coverImageUrl)`
      * `Publish()` $\rightarrow$ Validate điều kiện xuất bản (phải có ít nhất 1 waypoint).
      * `Unpublish()` $\rightarrow$ Quay lại Draft.
      * `AddWaypoint(dayNumber, name, address, lng, lat, notes)`
      * `UpdateWaypoint(waypointId, name, address, lng, lat, notes)`
      * `RemoveWaypoint(waypointId)` $\rightarrow$ Tự động dồn lại `order_index` liên tục trong ngày.
      * `ReorderWaypointsInDay(dayNumber, List<Guid> orderedWaypointIds)`
      * `SetDaySubtitle(dayNumber, subtitle)`
      * `RecalculateDistances(IGisDistanceCalculator calculator)`
  * `ItineraryDay` (Entity trong Aggregate):
    * `Id` (Guid), `ItineraryId` (Guid), `DayNumber` (int $\ge 1$), `Subtitle` (string?, $\le 100$ ký tự), `DayDistanceKm` (double).
  * `Waypoint` (Entity trong Aggregate):
    * `Id` (Guid), `ItineraryId` (Guid), `DayNumber` (int $\ge 1$), `OrderIndex` (int $\ge 0$), `Name` (string, $1-200$ ký tự), `Address` (string?), `Location` (`Wgs84Coordinate` VO hoặc `Point`), `Notes` (string?), `CreatedAt`, `UpdatedAt`.
  * `Value Objects`:
    * `ItineraryTitle`: Chuẩn hóa trim khoảng trắng thừa, validate $1-100$ ký tự, cấm chuỗi rỗng.
    * `Wgs84Coordinate`: Kinh độ `[-180, 180]`, Vĩ độ `[-90, 90]`.
  * `Domain Events`:
    * `ItineraryCreatedEvent`, `ItineraryPublishedEvent`, `WaypointAddedEvent`, `WaypointsReorderedEvent`.
  * `Abstractions/External Ports`:
    * `IGisDistanceCalculator`: Định nghĩa hàm tính khoảng cách giữa 2 tọa độ WGS84 và tính tổng lộ trình ngày.

---

### BƯỚC 2: Tầng `Tripory.Application` (CQRS UseCases & Orchestration)
* **Tổ chức thư mục:** `Tripory.Application/UserCases/V1/Itineraries/`
* **Commands:**
  * `CreateQuickDraftItineraryCommand` (Title)
  * `UpdateItineraryMetadataCommand` (Id, Title, Description, StartDate, CoverImageUrl)
  * `PublishItineraryCommand` (Id)
  * `DeleteItineraryCommand` (Id)
  * `AddWaypointCommand` (ItineraryId, DayNumber, Name, Address, Lng, Lat, Notes)
  * `UpdateWaypointCommand` (ItineraryId, WaypointId, Name, Address, Lng, Lat, Notes)
  * `DeleteWaypointCommand` (ItineraryId, WaypointId)
  * `ReorderWaypointsCommand` (ItineraryId, DayNumber, List<Guid> OrderedWaypointIds)
  * `SetDaySubtitleCommand` (ItineraryId, DayNumber, Subtitle)
  * `UploadCoverImageCommand` (ItineraryId, IFormFile File)
* **Queries:**
  * `GetItineraryByIdQuery` (Id)
  * `GetMyItinerariesQuery` (UserId, PageIndex, PageSize, IsPublicFilter)
* **DTOs & Responses:**
  * `ItineraryDetailResponse`, `ItinerarySummaryResponse`, `WaypointResponse`, `ItineraryDayResponse`.
* **Validators (FluentValidation):**
  * Bám sát các quy tắc `BR_01` đến `BR_04` của tài liệu đặc tả.

---

### BƯỚC 3: Tầng `Tripory.Persistence` (PostgreSQL PostGIS NetTopologySuite)
* **EF Core Configurations:**
  * `ItineraryConfiguration`: Khóa ngoại `UserId` liên kết bảng `users`, map `ItineraryTitle` qua Value Converter, quan hệ 1-N với `ItineraryDay` và `Waypoint`.
  * `ItineraryDayConfiguration`: Unique constraint `(itinerary_id, day_number)`.
  * `WaypointConfiguration`: Cấu hình cột `location` kiểu `geometry(Point, 4326)` qua NetTopologySuite, index không gian GIST: `HasIndex(w => w.Location).HasMethod("GIST")`.
* **Repository:**
  * `IItineraryRepository` & `ItineraryRepository`:
    * `GetByIdAsync(id, includeChildren, ct)`
    * `GetMyItinerariesPagedAsync(...)`
    * `AddAsync`, `UpdateAsync`, `DeleteAsync`.
* **EF Core Migration:**
  * Tạo Migration: `AddItineraryAndWaypointsTables`.

---

### BƯỚC 4: Tầng `Tripory.Infrastructure` (GIS Engine & Storage)
* **GIS Adapter:**
  * `GisDistanceCalculator`: Triển khai `IGisDistanceCalculator` sử dụng NetTopologySuite trắc địa / Haversine, tính khoảng cách mét và ki-lô-mét.
* **Storage Adapter:**
  * Mở rộng `LocalImageStorageService` để lưu trữ ảnh bìa $\le 10\text{MB}$, kiểm tra MIME types (`image/jpeg`, `image/png`, `image/webp`), lưu thư mục `uploads/covers/`.

---

### BƯỚC 5: Tầng `Tripory.API` (Controllers & Wiring)
* **Controller:** `ItinerariesController` (`/api/v1/itineraries`):
  * `POST /api/v1/itineraries` $\rightarrow$ Khởi tạo nhanh bản nháp.
  * `GET /api/v1/itineraries/{id}` $\rightarrow$ Chi tiết lịch trình kèm danh sách ngày & waypoints.
  * `GET /api/v1/itineraries/mine` $\rightarrow$ Danh sách lịch trình cá nhân (phân trang).
  * `PUT /api/v1/itineraries/{id}` $\rightarrow$ Cập nhật thông tin tổng quan.
  * `PUT /api/v1/itineraries/{id}/publish` $\rightarrow$ Xuất bản hành trình.
  * `DELETE /api/v1/itineraries/{id}` $\rightarrow$ Xóa mềm hành trình.
  * `POST /api/v1/itineraries/{id}/cover-image` $\rightarrow$ Tải ảnh bìa.
  * `POST /api/v1/itineraries/{id}/waypoints` $\rightarrow$ Thêm điểm dừng chân mới.
  * `PUT /api/v1/itineraries/{id}/waypoints/{waypointId}` $\rightarrow$ Chỉnh sửa điểm dừng chân.
  * `DELETE /api/v1/itineraries/{id}/waypoints/{waypointId}` $\rightarrow$ Xóa điểm dừng chân.
  * `PUT /api/v1/itineraries/{id}/days/{dayNumber}/reorder-waypoints` $\rightarrow$ Kéo thả sắp xếp thứ tự điểm trong ngày.
  * `PUT /api/v1/itineraries/{id}/days/{dayNumber}/subtitle` $\rightarrow$ Đặt phụ đề cho ngày.
  * `POST /api/v1/itineraries/demo-reset` $\rightarrow$ Khởi tạo lịch trình mẫu Hà Nội - Hà Giang 3 ngày.

---

### BƯỚC 6: Kiểm Thử & Nghiệm Thu (Quality Gate)
* `dotnet build` biên dịch sạch $100\%$ không lỗi, không warning (`TreatWarningsAsErrors=true`).
* Migration PostGIS sinh schema chuẩn và ánh xạ tọa độ không gian chính xác.
* Sẵn sàng chuyển giao cho Frontend tích hợp Mapbox / Leaflet và Dòng thời gian Timeline.
