# ARCHITECTURE.md – Bản Thiết Kế Kiến Trúc Backend Dự Án Tripory

Tài liệu này mô tả chi tiết kiến trúc kỹ thuật hệ thống backend cho **Tripory**, kế thừa các chuẩn mực thiết kế Clean Architecture, Domain-Driven Design (DDD) và CQRS từ `tc-ems-be`.

---

## 1. Bản Đồ Tổng Thể Kiến Trúc (Architecture Overview)

Kiến trúc backend của Tripory tuân thủ nghiêm ngặt **Mô hình Củ hành (Onion Architecture)** và **Quy tắc Phụ thuộc Đơn hướng (Unidirectional Dependency Rule)**:

```
                      ┌───────────────────────────────────────────────┐
                      │                 Tripory.API                   │
                      │       (HTTP Controllers, Middlewares, DI)     │
                      └──────────────────────┬────────────────────────┘
                                             │
                       ┌─────────────────────┴─────────────────────┐
                       ▼                                           ▼
       ┌───────────────────────────────┐           ┌───────────────────────────────┐
       │     Tripory.Infrastructure    │           │      Tripory.Persistence      │
       │ (S3, Redis, External Adapters)│           │ (EF Core, PostGIS, Migrations)│
       └───────────────┬───────────────┘           └───────────────┬───────────────┘
                       │                                           │
                       └─────────────────────┬─────────────────────┘
                                             ▼
                      ┌───────────────────────────────────────────────┐
                      │              Tripory.Application              │
                      │    (UseCases, Handlers, Validators, DTOs)     │
                      └──────────────────────┬────────────────────────┘
                                             ▼
                      ┌───────────────────────────────────────────────┐
                      │                Tripory.Domain                 │
                      │  (Entities, Value Objects, Domain Exceptions) │
                      └──────────────────────┬────────────────────────┘
                                             ▼
                      ┌───────────────────────────────────────────────┐
                      │             BuildingBlocks.Core               │
                      │ (BaseEntity, ValueObject base, Result<T>, CQRS)│
                      └───────────────────────────────────────────────┘
```

---

## 2. Phân Tầng Trách Nhiệm Chi Tiết (Layer Responsibilities)

### 2.1. `BuildingBlocks.Core` (Nền Tảng Độc Lập)
* **Trách nhiệm:** Cung cấp các primitives cơ sở cho toàn bộ hệ thống, hoàn toàn không phụ thuộc nghiệp vụ Tripory và không phụ thuộc third-party framework.
* **Chứa gì:**
  * `EntityBase<TKey>`, `EntityAuditBase<TKey>`, `EntityFullAuditBase<TKey>`.
  * `ValueObject` (base class hỗ trợ so sánh cấu trúc structural equality).
  * `Result`, `Result<T>` (Result pattern chuẩn hóa phản hồi).
  * `ICommand`, `ICommand<T>`, `IQuery<T>`, `ICommandHandler<T>`, `IQueryHandler<T, R>`.

### 2.2. `Tripory.Domain` (Lõi Nghiệp Vụ - Pure C#)
* **Trách nhiệm:** Trái tim của hệ thống. Chứa toàn bộ quy tắc nghiệp vụ bất biến (invariants) và máy trạng thái (state machines).
* **Quy tắc vàng:** **0% third-party dependencies**. Không tham chiếu EF Core, không HttpClient, không ASP.NET Core, không dùng Data Annotations.
* **Cấu trúc:**
  * `Entities/`: `Itinerary`, `Waypoint`, `User`, `ForkCredit`.
  * `ValueObjects/`: `Wgs84Coordinate` (tự validate lng/lat), `ItineraryTitle` (1-100 ký tự), `DateRange`.
  * `Enums/`: `ItineraryStatus` (Draft = 0, Published = 1), `WaypointCategory`.
  * `Exceptions/`: Các domain exception chuyên biệt kế thừa từ `NotFoundException`, `BadRequestException`.
  * `Abstractions/External/`: Các Port (interface) theo Dependency Inversion Principle (DIP) để Domain giao tiếp ra ngoài (ví dụ: `IGisDistanceCalculator`).
  * `Abstractions/Services/`: Các Domain Service thuần túy (không IO).

### 2.3. `Tripory.Application` (Điều Phối Use Cases - Orchestration)
* **Trách nhiệm:** Điều phối luồng dữ liệu (orchestration). Nhận command/query, gọi domain để kiểm tra và biến đổi state, gọi infrastructure để đọc/ghi, map kết quả sang DTO.
* **Quy tắc vàng:** **Không chứa business rules** và **không trực tiếp chạm vào EF Core**.
* **Cấu trúc (Tổ chức theo Feature Vertical Slice):**
  ```text
  Tripory.Application/UserCases/V1/
  ├── Itineraries/
  │   ├── Commands/
  │   ├── Queries/
  │   ├── Handlers/
  │   ├── Validators/
  │   ├── Responses/
  │   └── Extensions/
  ├── Social/
  │   └── (Fork, Like, Comment)...
  └── Users/
      └── (Profile, Achievements)...
  ```

### 2.4. `Tripory.Persistence` (Truy Cập Dữ Liệu & PostGIS)
* **Trách nhiệm:** Hiện thực hóa lưu trữ dữ liệu qua Entity Framework Core và PostgreSQL PostGIS.
* **Chứa gì:**
  * `ApplicationDbContext`.
  * `Configurations/`: Toàn bộ mapping Fluent API triển khai `IEntityTypeConfiguration<T>` (cấu hình bảng, schema, cột WGS84 PostGIS `geometry(Point, 4326)`, index, relations).
  * `Migrations/`: Code-First Migrations quản lý lịch sử schema.
  * `Repositories/`: Triển khai `IRepositoryBase<T, TKey>` và repository chuyên biệt.

### 2.5. `Tripory.Infrastructure` (Tích Hợp Dịch Vụ Bên Ngoài)
* **Trách nhiệm:** Hiện thực hóa các dịch vụ IO và giao tiếp bên ngoài.
* **Chứa gì:**
  * Triển khai các port `Domain/Abstractions/External/` (ví dụ GIS distance adapter).
  * Triển khai các service abstractions của Application (AWS S3 file storage, Redis Cache, Email Service, Image Processing via SixLabors.ImageSharp).

### 2.6. `Tripory.API` (Cổng Giao Tiếp HTTP - Composition Root)
* **Trách nhiệm:** Tiếp nhận HTTP Request, xác thực JWT, phân quyền RBAC, chuyển tiếp tới Application qua MediatR `Sender.Send`, xử lý response và exception toàn cục.
* **Chứa gì:**
  * `Controllers/V1/`: Controller mỏng kế thừa `ApiController`.
  * `Middlewares/`: Global Exception Handler (`IExceptionHandler`), Serilog Request Logging, CORS.
  * `Program.cs`: Nơi duy nhất ráp nối Dependency Injection (Composition Root).

---

## 3. Các Luồng Thực Thi Chuẩn (Standard Execution Flows)

### 3.1. Luồng Ghi (Mutation Flow - Command)
```
[Client] 
   │  (HTTP POST / PUT)
   ▼
[API Controller] 
   │  Sender.Send(command)
   ▼
[ValidationPipelineBehavior] ──(Lỗi validation)──> Trả Result.Failure(400)
   │  (Hợp lệ)
   ▼
[Command Handler] (Orchestration ONLY)
   │  1. Đọc entity từ Repository/Service
   │  2. Gọi Entity.Method() (Domain thực thi business rules & chuyển trạng thái)
   │  3. Ghi lại qua Repository/Service
   │  4. Map sang Response DTO
   ▼
[API Controller] 
   │  Kiểm tra if (result.IsFailure) return HandlerFailure(result);
   ▼
[Client] (HTTP 200 / 201 kèm ApiResponse<T>)
```

### 3.2. Luồng Đọc (Query Flow)
```
[Client] 
   │  (HTTP GET)
   ▼
[API Controller] 
   │  Sender.Send(query)
   ▼
[Query Handler]
   │  Đọc dữ liệu tối ưu qua IQueryable / Read Repository / Dapper / Redis Cache
   │  Map trực tiếp sang Response DTO (Projection)
   ▼
[Client] (HTTP 200 kèm DTO)
```

---

## 4. Nguyên Tắc Thiết Kế API & RESTful

1. **HTTP Verbs:**
   * `GET`: Truy vấn dữ liệu, an toàn, có thể cache (Redis).
   * `POST`: Tạo mới tài nguyên (`/api/v1/itineraries`).
   * `PUT`: Cập nhật toàn phần hoặc **thay đổi trạng thái** (`PUT /api/v1/itineraries/{id}/status`).
   * `DELETE`: Xóa tài nguyên (thực hiện Soft-Delete qua Global Query Filter `is_deleted = false`).
   * **Cấm:** Không dùng `PATCH` trong toàn bộ hệ sinh thái Tripory.
2. **Response Chuẩn Hóa:**
   ```json
   {
     "status": "success",
     "data": { ... },
     "message": "Hành trình đã được xuất bản thành công",
     "timestamp": "2026-09-12T13:30:00Z"
   }
   ```
3. **HTTP Status Codes:**
   * `200 OK`: Thành công với dữ liệu trả về.
   * `201 Created`: Tạo mới thành công kèm resource ID.
   * `400 Bad Request`: Lỗi validation DTO hoặc vi phạm nghiệp vụ.
   * `401 Unauthorized`: Chưa đăng nhập / Token hết hạn.
   * `403 Forbidden`: Không có quyền truy cập / Không phải tác giả của chuyến đi.
   * `404 Not Found`: Không tìm thấy tài nguyên.
   * `500 Internal Server Error`: Lỗi hệ thống không mong muốn (được bắt và log bởi Serilog).
