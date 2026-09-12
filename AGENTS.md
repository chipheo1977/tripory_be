# AGENTS.md – Quy Tắc & Hướng Dẫn Kỹ Thuật Dự Án Tripory Backend

Tài liệu này định hình vai trò, nguyên tắc làm việc và quy chuẩn kỹ thuật bắt buộc cho mọi Developer và AI Agent hoạt động trong repository `tripory_be`. Mọi đề xuất và dòng code phải tuân thủ nghiêm ngặt các điều khoản dưới đây (kế thừa tiêu chuẩn kiến trúc enterprise từ `tc-ems-be`).

---

## 1. Vai Trò & Nguyên Tắc Làm Việc Cốt Lõi (Core Principles)

* **Vai trò của AI:** **Senior Software Architect & Technical Advisor** (Cố vấn Kỹ thuật Cấp cao).
* **Vai trò của Người dùng (User):** **Tech Lead / Product Owner** — Người nắm toàn quyền quyết định về thiết kế, kiến trúc và phê duyệt từng bước triển khai.
* **Nguyên tắc "Anti-Vibe Coding":**
  1. **Tuyệt đối không tự ý viết code hàng loạt:** Không sinh code trước khi giải pháp thiết kế được User phê duyệt.
  2. **Không phỏng đoán nghiệp vụ:** Mọi yêu cầu nghiệp vụ bắt buộc phải đối chiếu và bám sát tài liệu đặc tả tại `../tripory/docs` (BRD và các tài liệu thành phần).
  3. **Giải thích "Tại sao" trước khi đưa "Làm thế nào":** Mọi lựa chọn công nghệ, pattern hay cấu trúc dữ liệu đều phải chỉ rõ ưu điểm, nhược điểm và lý do lựa chọn.

---

## 2. Quy Trình Trao Đổi Bắt Buộc 4 Bước (Advisory Protocol)

```
[1. Phân tích Yêu cầu] ──> [2. Đề xuất & So sánh Phương án] ──> [3. Chờ User Chốt] ──> [4. Hướng dẫn Từng Bước Nhỏ]
```

1. **Bước 1 – Khảo sát & Phân tích (Analyze):** Đọc và trích dẫn yêu cầu liên quan trong `../tripory/docs`. Làm rõ scope và constraints.
2. **Bước 2 – Đề xuất 2-3 Phương án & So sánh (Trade-offs):** Trình bày ít nhất 2 phương án kèm bảng so sánh Pros/Cons/Complexity. Đưa ra Khuyến nghị cá nhân của AI.
3. **Bước 3 – Cổng phê duyệt (Approval Gate):** Dừng lại và chờ User phê duyệt phương án.
4. **Bước 4 – Hướng dẫn Từng Bước Nhỏ (Micro-step Execution):** Cung cấp lệnh CLI cụ thể, viết từng file nhỏ gọn, giải thích code trọng yếu, kèm tiêu chuẩn xác nhận (Verification).

---

## 3. Khung Kiến Trúc Dự Án (Clean Architecture + DDD + CQRS)

Kế thừa mô hình phân tách dự án enterprise từ `tc-ems-be`, solution được tổ chức rõ ràng thành 2 khối:

```text
tripory_be/
├── src/
│   ├── BuildingBlocks/
│   │   └── Core/                     # Class Library: BaseEntity, ValueObject, Result<T>, CQRS abstractions
│   │
│   └── Services/Tripory/             # Bounded Context chính của Tripory
│       ├── Tripory.Domain/           # Class Library: Entities, VOs, Domain Services, Exceptions, Ports (Pure C#)
│       ├── Tripory.Application/      # Class Library: UseCases, Handlers (Orchestration), Validators, DTOs
│       ├── Tripory.Persistence/      # Class Library: DbContext, Fluent Configurations, PostGIS Migrations
│       ├── Tripory.Infrastructure/   # Class Library: S3, Redis, External Adapters, Implementation của Ports
│       └── Tripory.API/              # Web API: Controllers V1, Middlewares, DI Setup, Program.cs
│
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
├── tripory_be.sln
├── AGENTS.md
└── ARCHITECTURE.md
```

* **Chỉ sử dụng CLI mặc định:** Solution và các Project khởi tạo bằng `dotnet new` tiêu chuẩn. Không dùng template bên thứ ba.
* **Nguyên tắc YAGNI:** Chỉ cài thêm NuGet package khi bước hiện tại thực sự cần đến.

---

## 4. Quy Tắc Phân Tầng Cốt Lõi (Layer Responsibilities & Boundary Rules)

Mỗi tầng có **duy nhất một trách nhiệm**. Tuyệt đối không để logic lẫn lộn giữa các tầng:

| Tầng | Trách nhiệm cốt lõi | Chứa gì | Được phụ thuộc | TUYỆT ĐỐI KHÔNG ĐƯỢC LÀM |
|---|---|---|---|---|
| **Domain** | **Business Logic & Invariants** | Entity + Invariant + State machine, **Value Object**, Domain Service (pure), Domain Event, Domain Exceptions, **Abstractions/External** (Ports) | BCL + `BuildingBlocks.Core` (Pure C#) | ❌ EF Core, `HttpClient`, IO, file system.<br>❌ Data annotations (`[Table]`, `[Column]`). |
| **Application** | **Orchestration** (Điều phối logic) | Command/Query, Validator, **Handler chỉ orchestrate**, Response DTO, Mapping, Service Abstractions | `Tripory.Domain` | ❌ Chứa business rule (`if (status == X) throw...`).<br>❌ Trực tiếp inject `DbContext`, `DbSet`, hoặc EF types. |
| **Persistence** | **Data Access** | `ApplicationDbContext`, `Configurations/` (Fluent API), `Migrations/`, Repositories | EF Core, PostgreSQL, NetTopologySuite, `Domain` | ❌ Chứa business logic. |
| **Infrastructure** | **IO & External Services** | Triển khai interface từ Application/Domain; AWS S3, Redis, Mail, Geocoding external API | `Application`, `Domain`, `Persistence` (khi cần) | ❌ Quyết định business rule (delegate lên Domain). |
| **API** | **HTTP Gateway & Composition Root** | Controllers V1 (`Sender.Send`), Action Filters, Middlewares, `Program.cs` (Wiring DI) | Tất cả các tầng để wire DI | ❌ Business logic, direct data access. |

---

## 5. Pattern Xuất Sắc Kế Thừa Từ `tc-ems-be`

Mọi developer và AI Agent bắt buộc phải tuân theo 7 pattern sau khi triển khai code:

### Pattern 1: Tổ chức UseCases theo Feature Vertical Slice (trong `Application`)
Không gom chung toàn bộ commands hoặc queries vào một folder phẳng. Tổ chức theo từng feature domain:
```text
Tripory.Application/UserCases/V1/Itineraries/
├── Commands/          # Records: CreateItineraryCommand, UpdateItineraryCommand...
├── Queries/           # Records: GetItineraryByIdQuery, GetDiscoveryFeedQuery...
├── Handlers/          # 1 file cho 1 handler: CreateItineraryCommandHandler.cs
├── Validators/        # FluentValidation: CreateItineraryCommandValidator.cs
├── Responses/         # Response DTOs
└── Extensions/        # Extension methods mapping Entity -> Response DTO
```

### Pattern 2: Handler CHỈ làm Orchestration (Không chứa Business Rules)
Handler chỉ thực hiện 4 bước điều phối tuần tự:
1. **IO:** Gọi service/repository đọc dữ liệu.
2. **Domain:** Gọi method trên Entity hoặc Domain Service để áp rules và đổi trạng thái.
3. **IO:** Gọi service/repository lưu dữ liệu.
4. **Output:** Map kết quả sang Response DTO và trả về `Result<T>`.

```csharp
// Ví dụ: Handler chỉ điều phối, Domain Entity tự bảo vệ invariant
public async Task<Result> Handle(PublishItineraryCommand request, CancellationToken ct)
{
    var itinerary = await _itineraryRepository.GetByIdAsync(request.Id, ct)
        ?? throw new ItineraryNotFoundException(request.Id);

    // Business rule thuộc về Domain Entity:
    itinerary.Publish();

    await _itineraryRepository.UpdateAsync(itinerary, ct);
    return Result.Success();
}
```

### Pattern 3: Value Object cho các khái niệm có Invariant (Rich Domain)
Tạo Value Object khi một hoặc nhiều field có invariant hoặc rule format lặp lại:
* `Wgs84Coordinate`: Tự validate `lng` trong `[-180, 180]`, `lat` trong `[-90, 90]`.
* `ItineraryTitle`: Tự trim khoảng trắng, kiểm tra độ dài `1 - 100` ký tự.
* `DateRange`: Tự kiểm tra `StartDate <= EndDate`.
* **Quy tắc:** Field đơn lẻ không có invariant (như ghi chú tự do `string Note`) thì giữ primitive thuần, không lạm dụng bọc VO.

### Pattern 4: Tách Interface (Abstractions/Ports) theo DIP qua biên Domain ↔ Infrastructure
* Khi Domain cần một dịch vụ kỹ thuật từ bên ngoài (ví dụ: công thức trắc địa GIS phức tạp, tra cứu bên ngoài):
  * **Port (Interface):** Đặt trong `Tripory.Domain/Abstractions/External/IGisDistanceCalculator.cs` (Domain sở hữu interface).
  * **Adapter (Implementation):** Đặt trong `Tripory.Infrastructure/Implementations/Gis/GisDistanceCalculator.cs` (Infra phụ thuộc Domain, Domain hoàn toàn pure).

### Pattern 5: Fluent API thuần túy trong `Persistence` (Không dùng Data Annotations)
* Tuyệt đối không dùng `[Table]`, `[Column]`, `[Key]` trên Domain Entities.
* Toàn bộ mapping đặt trong `Tripory.Persistence/Configurations/<Entity>Configuration.cs` triển khai `IEntityTypeConfiguration<T>`.
* Value Object ánh xạ qua `HasConversion` (cho single value) hoặc `OwnsOne` (cho multi-value).

### Pattern 6: CQRS + MediatR + Pipeline Validation
* Command/Query implement `ICommand` / `ICommand<T>` / `IQuery<T>`.
* Handler implement `ICommandHandler<T>` / `IQueryHandler<T, R>`.
* `ValidationPipelineBehavior` tự bắt lỗi FluentValidation và trả về `Result<T>.Failure(...)`.
* Controller mỏng, kiểm tra:
  ```csharp
  var result = await Sender.Send(command);
  if (result.IsFailure) return HandlerFailure(result);
  return Ok(result);
  ```

### Pattern 7: Quy chuẩn HTTP Verb & REST
* Mọi thao tác làm thay đổi trạng thái (State-changing mutations) dùng **`PUT`** (ví dụ: `PUT /api/v1/itineraries/{id}/status`). **Tuyệt đối không dùng PATCH**.
* Naming convention cho status change: `ChangeStatus<Entity>...`.

---

## 6. Danh Sách Điều Cấm (What NOT To Do Checklist)

* ❌ **Cấm:** Viết business rule (`if (status == X) throw...`) trong **Handler** $\rightarrow$ Phải chuyển thành method trên Entity (vd `itinerary.Publish()`).
* ❌ **Cấm:** Viết business rule trong **Infrastructure Service** $\rightarrow$ Service chỉ làm IO; logic phải gọi xuống Domain.
* ❌ **Cấm:** Inject `ApplicationDbContext`, `DbSet<T>` vào **Handler** $\rightarrow$ Phải dùng Repository Interface hoặc Service Abstraction.
* ❌ **Cấm:** Import `Microsoft.EntityFrameworkCore`, `HttpClient`, hay web types vào **`Tripory.Domain`**.
* ❌ **Cấm:** Dùng Data Annotations (`[Column]`, `[Table]`) trong Domain Entity $\rightarrow$ Phải dùng Fluent API trong Persistence.
* ❌ **Cấm:** Viết Controller hoặc Migration khi Domain Model chưa được phê duyệt.
* ❌ **Cấm:** Quên ghi log `_logger.LogInformation(...)` đầu mỗi action trong Controller.
* ❌ **Cấm:** Quên kiểm tra `if (result.IsFailure) return HandlerFailure(result);` trước khi `Ok(result)`.

---

## 7. Tiêu Chuẩn Nghiệm Thu & Kiểm Chứng (Quality Gate)

Mỗi bước code xong phải thỏa mãn:
1. Lệnh `dotnet build` chạy thành công không có warning/error.
2. Dependency graph tuân thủ nghiêm ngặt 1 chiều: `API` $\rightarrow$ `Infrastructure`/`Persistence` $\rightarrow$ `Application` $\rightarrow$ `Domain` $\rightarrow$ `BuildingBlocks.Core`. Domain và BuildingBlocks không reference ngược.
3. Toàn bộ logic bám sát đặc tả tại `../tripory/docs`.
