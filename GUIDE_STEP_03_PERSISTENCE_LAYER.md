# HƯỚNG DẪN TỰ CODE BƯỚC 3: TẦNG PERSISTENCE & POSTGIS MIGRATIONS
## Module: ITINERARY_01_MAP+TIMELINE (Lưu Trữ Dữ Liệu & Không Gian Địa Lý PostGIS)

> **Dành cho:** Tech Lead / Developer tự tay triển khai code  
> **Vị trí lưu:** `C:\Users\OS\Documents\personal\tripory_be\GUIDE_STEP_03_PERSISTENCE_LAYER.md`  
> **Nguyên tắc cốt lõi:** Không sinh code sẵn hàng loạt; cung cấp đặc tả **5W1H**, phân tích chuyên sâu **OOP, SOLID, DDD & PostGIS Spatial Design**, và các bước logic để bạn tự tay lập trình theo chuẩn **Clean Architecture + DDD**.

---

## 🗺️ Cây Thư Mục Các File Cần Triển Khai Trong Bước 3

```text
src/Services/Tripory/Tripory.Persistence/
├── Configurations/
│   ├── ItineraryConfiguration.cs          # FILE 01: Fluent API itineraries, HasConversion VOs, Soft Delete, Backing Fields
│   ├── ItineraryDayConfiguration.cs       # FILE 02: Fluent API itinerary_days, Unique Index (ItineraryId, DayNumber)
│   └── WaypointConfiguration.cs           # FILE 03: Fluent API waypoints, NetTopologySuite Point, Spatial GiST Index
│
├── ApplicationDbContext.cs                # FILE 04: Đăng ký DbSet<Itinerary>, DbSet<ItineraryDay>, DbSet<Waypoint>
│
├── Repositories/
│   ├── RepositoryBase.cs                  # FILE 05: Generic Base Repository triển khai IRepositoryBase<TEntity, TKey>
│   └── ItineraryRepository.cs             # FILE 06: Triển khai IItineraryRepository (GetByIdWithDetails, Paged, IsOwner)
│
├── DependencyInjection.cs                 # FILE 07: Đăng ký DI cho IItineraryRepository
│
└── Migrations/
    └── YYYYMMDD_Add_Itinerary_PostGIS_Tables.cs # FILE 08: PostGIS EF Core Migration sinh tự động qua CLI
```

---

## PHẦN 1: FLUENT API CONFIGURATIONS (CẤU HÌNH THỰC THỂ EF CORE)

---

### FILE 01: `ItineraryConfiguration.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** EF Core Change Tracker và DbContext sử dụng file này để ánh xạ bảng cơ sở dữ liệu.
* **What (Là gì):** Cấu hình Entity Type Configuration cho Aggregate Root `Itinerary` theo Fluent API.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Persistence/Configurations/ItineraryConfiguration.cs`.
* **When (Khi nào):** Nạp tự động vào ModelBuilder lúc ứng dụng khởi động (`modelBuilder.ApplyConfigurationsFromAssembly`).
* **Why (Tại sao):** Kế thừa Pattern 5 từ `AGENTS.md`: Tách biệt 100% chi tiết kỹ thuật của Database (tên bảng, độ dài cột, khóa ngoại, chỉ mục) ra khỏi Domain Entity; tuyệt đối không dùng Data Annotations (`[Table]`, `[Column]`) trên Domain.
* **How (Như thế nào):** Triển khai `IEntityTypeConfiguration<Itinerary>`, cấu hình bảng `itineraries` thuộc schema `"itinerary"`, ánh xạ các Value Object qua `HasConversion`, cấu hình Backing Fields và Global Query Filter cho Soft Delete.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong File Này
* **Encapsulation (Đóng gói Backing Field):**
  * Domain Entity `Itinerary` đóng gói danh sách `_days` và `_waypoints` qua `private readonly List<T>` và chỉ để lộ `IReadOnlyCollection<T>`.
  * Configuration cần chỉ định:
    ```csharp
    builder.Navigation(i => i.Days).UsePropertyAccessMode(PropertyAccessMode.Field);
    builder.Navigation(i => i.Waypoints).UsePropertyAccessMode(PropertyAccessMode.Field);
    ```
    EF Core sẽ đọc/ghi trực tiếp vào private backing field mà không phá vỡ tính đóng gói của Domain.
* **Value Object Conversion (DDD Mapping):**
  * `Title` ánh xạ qua `.HasConversion(t => t.Value, v => ItineraryTitle.Create(v).Value)`.
  * `Description` là nullable VO, ánh xạ qua `.HasConversion(d => d != null ? d.Value : null, v => v != null ? ItineraryDescription.Create(v).Value : null)`.
* **Soft Delete Global Query Filter:**
  * Áp dụng `builder.HasQueryFilter(i => !i.IsDeleted);` để toàn bộ các câu lệnh LINQ đọc dữ liệu tự động bỏ qua các bản ghi đã xóa mềm theo chuẩn `EntityFullAuditBase`.

#### 3. Định Hướng & Tiêu Chuẩn Tự Code
* **Schema & Table:** `builder.ToTable("itineraries", "itinerary");`
* **Primary Key:** `builder.HasKey(i => i.Id);`
* **Column Types & Constraints:**
  * `UserId`: `IsRequired()`. Đánh index để tăng tốc truy vấn danh sách chuyến đi của user.
  * `Title`: `HasMaxLength(100)`, `IsRequired()`.
  * `Description`: `HasMaxLength(1000)`.
  * `CoverImageUrl`: `HasMaxLength(500)`.
  * `StartDate`: `HasColumnType("date")`.
  * `IsPublic`: `HasDefaultValue(false)`, `IsRequired()`.
  * `TotalDistanceKm`: `HasDefaultValue(0.0)`, `IsRequired()`.
* **Quan hệ 1-N:**
  * `HasMany(i => i.Days).WithOne().HasForeignKey(d => d.ItineraryId).OnDelete(DeleteBehavior.Cascade)`.
  * `HasMany(i => i.Waypoints).WithOne().HasForeignKey(w => w.ItineraryId).OnDelete(DeleteBehavior.Cascade)`.

---

### FILE 02: `ItineraryDayConfiguration.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Quản lý cấu hình lưu trữ các mốc ngày (Day 1, Day 2...) thuộc hành trình.
* **What (Là gì):** Entity Type Configuration cho thực thể `ItineraryDay`.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Persistence/Configurations/ItineraryDayConfiguration.cs`.
* **When (Khi nào):** Ánh xạ cấu trúc bảng `itinerary_days` khi build DbModel.
* **Why (Tại sao):** Đảm bảo mỗi ngày trong một chuyến đi là duy nhất (`Unique DayNumber`), tránh trùng lặp dữ liệu ngày.
* **How (Như thế nào):** Triển khai `IEntityTypeConfiguration<ItineraryDay>`, tạo Composite Unique Index trên `(itinerary_id, day_number)`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong File Này
* **Entity Invariant Persistence (Bảo toàn Invariant):**
  * Trong một chuyến đi, không thể có 2 mốc "Ngày 1".
  * Index: `builder.HasIndex(d => new { d.ItineraryId, d.DayNumber }).IsUnique();` ép cơ sở dữ liệu làm lớp phòng thủ kiên cố nhất.
* **SRP:** File này chỉ chịu trách nhiệm duy nhất là định nghĩa cấu trúc bảng cho thực thể mốc ngày.

#### 3. Định Hướng & Tiêu Chuẩn Tự Code
* **Schema & Table:** `builder.ToTable("itinerary_days", "itinerary");`
* **Primary Key:** `builder.HasKey(d => d.Id);`
* **Columns:**
  * `ItineraryId`: `IsRequired()`.
  * `DayNumber`: `IsRequired()`.
  * `Subtitle`: `HasMaxLength(100)`.
  * `DayDistanceKm`: `HasDefaultValue(0.0)`, `IsRequired()`.

---

### FILE 03: `WaypointConfiguration.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Lưu trữ các điểm dừng chân trên bản đồ và timeline của chuyến đi.
* **What (Là gì):** Entity Type Configuration cho thực thể `Waypoint`, cấu hình trường không gian PostGIS `geometry(Point, 4326)` và GiST Index.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Persistence/Configurations/WaypointConfiguration.cs`.
* **When (Khi nào):** EF Core sinh bảng `waypoints` kèm hỗ trợ truy vấn địa lý không gian.
* **Why (Tại sao):** Tuân thủ `BR_01` trong `create map+timeline.md`: Tọa độ WGS84 cần được lưu trữ dưới dạng PostGIS Spatial Geometry chuẩn SRID 4326 để hỗ trợ tìm kiếm bán kính (ST_DWithin), bounding box (ST_Intersects) và tính cự ly tốc độ cao.
* **How (Như thế nào):** Chuyển đổi Value Object `Wgs84Coordinate` thành `NetTopologySuite.Geometries.Point` và đánh chỉ mục không gian `gist`.

#### 2. Biểu Diễn OOP, SOLID & DDD Trong File Này
* **Impedance Mismatch Resolution (Cầu nối Lập trình hướng đối tượng ↔ Database Không gian):**
  * Trong C# Domain: Tọa độ là một Value Object bất biến thuần khiết `Wgs84Coordinate(Longitude, Latitude)`.
  * Trong PostgreSQL: Là kiểu dữ liệu hình học `geometry(Point, 4326)`.
  * Ánh xạ thông qua HasConversion:
    ```csharp
    builder.Property(w => w.Coordinate)
        .HasConversion(
            coord => new NetTopologySuite.Geometries.Point(coord.Longitude, coord.Latitude) { SRID = 4326 },
            point => Wgs84Coordinate.Create(point.X, point.Y).Value)
        .HasColumnType("geometry(Point, 4326)")
        .HasColumnName("location")
        .IsRequired();
    ```
    > [!IMPORTANT]
    > **Quy chuẩn Không Gian NetTopologySuite:** Trong GIS, tọa độ luôn có thứ tự `(X, Y)` tương ứng với `(Longitude, Latitude)` hay `(Kinh độ, Vĩ độ)`. Tuyệt đối không được đảo ngược vị trí!
* **Hiệu năng Chỉ mục Không Gian (Spatial Indexing):**
  * Tạo chỉ mục GiST (Generalized Search Tree):
    ```csharp
    builder.HasIndex(w => w.Coordinate).HasMethod("gist");
    ```
    Giúp tăng tốc các phép tính truy vấn không gian từ $\mathcal{O}(N)$ xuống $\mathcal{O}(\log N)$.
* **Composite Timeline Index:**
  * Tạo chỉ mục kết hợp: `builder.HasIndex(w => new { w.ItineraryId, w.DayNumber, w.OrderIndex });` phục vụ truy vấn hiển thị Timeline theo thứ tự cực nhanh.

#### 3. Định Hướng & Tiêu Chuẩn Tự Code
* **Schema & Table:** `builder.ToTable("waypoints", "itinerary");`
* **Columns:**
  * `Name`: Ánh xạ `WaypointName` qua `HasConversion`, `HasMaxLength(200)`, `IsRequired()`.
  * `Address`: `HasMaxLength(500)`.
  * `Notes`: `HasMaxLength(2000)`.
  * `DayNumber`, `OrderIndex`: `IsRequired()`.

---

## PHẦN 2: DB CONTEXT & REPOSITORIES

---

### FILE 04: `ApplicationDbContext.cs` (Cập nhật)

#### 1. Mô hình 5W1H
* **Who (Ai):** Composition Root của tầng Persistence.
* **What (Là gì):** Khai báo các `DbSet<T>` cho Itinerary aggregate.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Persistence/ApplicationDbContext.cs`.
* **Why (Tại sao):** Để EF Core nhận diện các Aggregate Root và Entity con của module Itinerary trong cây Model.
* **How (Như thế nào):** Bổ sung 3 DbSet:
  ```csharp
  public DbSet<Itinerary> Itineraries => Set<Itinerary>();
  public DbSet<ItineraryDay> ItineraryDays => Set<ItineraryDay>();
  public DbSet<Waypoint> Waypoints => Set<Waypoint>();
  ```

---

### FILE 05: `RepositoryBase.cs` (Generic Base Repository)

#### 1. Mô hình 5W1H
* **Who (Ai):** Lớp trừu tượng nền tảng cho mọi Repository trong Persistence.
* **What (Là gì):** Triển khai interface `IRepositoryBase<TEntity, TKey>` của `BuildingBlocks.Core.Abstractions.Persistence`.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Persistence/Repositories/RepositoryBase.cs`.
* **Why (Tại sao):** Triệt tiêu code lặp lại cho các thao tác CRUD cơ bản (`FindByIdAsync`, `FindSingleAsync`, `FindAll`, `AddAsync`, `UpdateAsync`, `RemoveAsync`).
* **How (Như thế nào):** Nhận generic `TEntity`, `TKey`, `TContext : DbContext`, thực thi các thao tác trên `_context.Set<TEntity>()`.

#### 2. Biểu Diễn OOP & SOLID
* **Inheritance & Generics (Kế thừa & Tham số hóa kiểu):**
  * `public abstract class RepositoryBase<TEntity, TKey, TContext> : IRepositoryBase<TEntity, TKey> where TEntity : class where TContext : DbContext`
* **LSP (Liskov Substitution Principle):** Mọi repository cụ thể (như `ItineraryRepository`) có thể thay thế hoàn hảo cho `IRepositoryBase<Itinerary, Guid>`.

---

### FILE 06: `ItineraryRepository.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Triển khai cổng lưu trữ `IItineraryRepository` từ Domain.
* **What (Là gì):** Concrete Repository chứa các phương thức truy vấn chuyên biệt của Itinerary.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Persistence/Repositories/ItineraryRepository.cs`.
* **When (Khi nào):** Được inject vào các Application Command/Query Handlers.
* **Why (Tại sao):** Thỏa mãn nguyên lý **DIP (Dependency Inversion Principle)**: Domain định nghĩa giao diện (Port), Persistence triển khai chi tiết kỹ thuật (Adapter).
* **How (Như thế nào):** Kế thừa `RepositoryBase<Itinerary, Guid, ApplicationDbContext>` và triển khai 3 phương thức chuyên biệt:
  1. `GetByIdWithDetailsAsync(Guid id, CancellationToken ct)`: Nạp kèm `.Include(i => i.Days).Include(i => i.Waypoints)`.
  2. `IsOwnerAsync(Guid itineraryId, Guid userId, CancellationToken ct)`: Kiểm tra nhanh quyền sở hữu bằng `.AnyAsync()`.
  3. `GetMyItinerariesPagedAsync(Guid userId, int pageIndex, int pageSize, bool? isPublic, CancellationToken ct)`: Truy vấn danh sách phân trang, hỗ trợ lọc trạng thái, sắp xếp `UpdatedAt DESC`.

#### 2. Biểu Diễn OOP & DDD
* **Eager Loading Boundary (Ranh giới nạp dữ liệu):**
  * Khi xử lý nghiệp vụ thay đổi trạng thái (Command), ta nạp toàn bộ Aggregate (`Include Days và Waypoints`) để Entity tự bảo vệ Invariant.
  * Khi truy vấn danh sách tóm tắt (Query phân trang), chỉ nạp dữ liệu cần thiết để tối ưu bộ nhớ Change Tracker.

---

### FILE 07: `DependencyInjection.cs` (Cập nhật)

#### 1. Mô hình 5W1H
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs`.
* **What (Là gì):** Đăng ký service `IItineraryRepository` với vòng đời `Scoped`:
  ```csharp
  services.AddScoped<IItineraryRepository, ItineraryRepository>();
  ```

---

## PHẦN 3: POSTGIS MIGRATIONS (DI CHUYỂN DỮ LIỆU)

---

### FILE 08: `Add_Itinerary_PostGIS_Tables` Migration

#### 1. Mô hình 5W1H
* **Who (Ai):** EF Core Tools CLI sinh mã tự động dựa trên snapshot của ModelBuilder.
* **What (Là gì):** File C# Migration định nghĩa các thao tác `Up` và `Down` (tạo schema `itinerary`, tạo bảng, tạo khóa ngoại, tạo spatial GiST index).
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Persistence/Migrations/`.
* **When (Khi nào):** Chạy lệnh CLI sau khi đã hoàn thành 7 file trên.
* **Why (Tại sao):** Quản lý phiên bản Database có tính lịch sử (Database Version Control), có thể triển khai đồng bộ trên mọi môi trường (Dev, Staging, Production).

#### 2. Lệnh CLI Thực Thi
* **Lệnh 1: Sinh Migration:**
  ```powershell
  dotnet ef migrations add Add_Itinerary_PostGIS_Tables --project src/Services/Tripory/Tripory.Persistence --startup-project src/Services/Tripory/Tripory.API --output-dir Migrations
  ```
* **Lệnh 2: Cập nhật Database (khi kết nối Postgres):**
  ```powershell
  dotnet ef database update --project src/Services/Tripory/Tripory.Persistence --startup-project src/Services/Tripory/Tripory.API
  ```

---

## 🏁 Tiêu Chuẩn Nghiệm Thu & Kiểm Chứng (Quality Gate Bước 3)

1. **Biên dịch Solution:**
   ```powershell
   dotnet build
   ```
   **Kỳ vọng:** `Build succeeded` với `0 Warning(s), 0 Error(s)`.

2. **Kiểm tra File Migration sinh ra:**
   * Có `migrationBuilder.EnsureSchema(name: "itinerary");`.
   * Bảng `waypoints` có cột `"location"` kiểu `geometry(Point, 4326)`.
   * Có spatial index: `CREATE INDEX "IX_waypoints_location" ON itinerary.waypoints USING gist (location);`.
   * Có Global Query Filter Soft-delete cho `itineraries`.

3. **Kiểm tra Dependency Graph:**
   * Tầng `Domain` và `Application` vẫn hoàn toàn thuần khiết, không bị lây nhiễm bất kỳ thư viện EF Core hay PostGIS nào.
   * Toàn bộ mapping PostGIS nằm trọn vẹn trong `Persistence`.
