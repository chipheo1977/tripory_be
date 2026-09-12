# AGENTS.md – Quy Tắc & Hướng Dẫn Kỹ Thuật Dự Án Tripory Backend

Tài liệu này định hình vai trò, nguyên tắc làm việc và quy trình kỹ thuật bắt buộc cho mọi AI Agent hoạt động trong repository `tripory_be`. Mọi đề xuất và hướng dẫn của AI phải tuân thủ nghiêm ngặt các điều khoản dưới đây.

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

Trước khi bắt tay vào bất kỳ bài toán hay tính năng nào, AI phải tuân thủ quy trình 4 bước:

```
[1. Phân tích Yêu cầu] ──> [2. Đề xuất & So sánh Phương án] ──> [3. Chờ User Chốt] ──> [4. Hướng dẫn Từng Bước Nhỏ]
```

1. **Bước 1 – Khảo sát & Phân tích (Analyze):**
   * Đọc và trích dẫn yêu cầu liên quan trong `../tripory/docs`.
   * Làm rõ phạm vi (In-scope, Out-of-scope) và các ràng buộc kỹ thuật.
2. **Bước 2 – Đề xuất 2-3 Phương án & So sánh (Trade-offs):**
   * Trình bày ít nhất 2 phương án khả thi.
   * Lập bảng so sánh rõ: Ưu điểm (Pros), Nhược điểm (Cons), Độ phức tạp (Complexity), Khả năng mở rộng (Scalability).
   * Đưa ra **Khuyến nghị cá nhân của AI** cùng lý do kỹ thuật thuyết phục.
3. **Bước 3 – Cổng phê duyệt (Approval Gate):**
   * Dừng lại và hỏi ý kiến User. Chỉ khi User đồng ý với phương án nào mới được tiếp tục.
4. **Bước 4 – Hướng dẫn Từng Bước Nhỏ (Micro-step Execution):**
   * Cung cấp lệnh CLI cụ thể để User chạy hoặc review.
   * Viết từng file nhỏ gọn, có type safety, chú thích rõ mục đích của các đoạn code trọng yếu.
   * Cung cấp tiêu chí kiểm thử/xác nhận (Verification) ngay sau mỗi bước.

---

## 3. Khởi Tạo Khung Dự Án Tối Thiểu (Minimal Scaffolding via Default CLI)

* **Chỉ sử dụng CLI mặc định:** Toàn bộ Solution và Project bắt buộc khởi tạo bằng công cụ `dotnet CLI` tiêu chuẩn của .NET SDK.
* **Không dùng template bên ngoài:** Nghiêm cấm dùng các template Clean Architecture cồng kềnh có sẵn trên internet (chứa sẵn hàng chục thư viện chưa cần đến).
* **Cấu trúc khung chuẩn (Clean Architecture):**
  ```text
  tripory_be/
  ├── src/
  │   ├── Core/
  │   │   ├── Domain/           # Class Library (Pure C#)
  │   │   └── Application/      # Class Library (Use Cases, Contracts)
  │   ├── Infrastructure/       # Class Library (PostgreSQL, EF Core, S3, Redis)
  │   └── Presentation/
  │       └── Api/              # ASP.NET Core Web API
  ├── tests/
  │   ├── UnitTests/
  │   └── IntegrationTests/
  ├── tripory_be.sln
  ├── AGENTS.md
  └── ARCHITECTURE.md
  ```
* **Nguyên tắc YAGNI (You Aren't Gonna Need It):** Chỉ cài thêm NuGet package khi bước hiện tại thực sự cần đến. Không cài đặt đón đầu.

---

## 4. Quy Trình Xây Dựng "Từ Trong Ra Ngoài" (Inside-Out Workflow)

Mọi module tính năng phải được phát triển tuần tự từ tầng lõi nghiệp vụ đi ra ngoài:

```
[Tầng 1: Domain] ──> [Tầng 2: Application] ──> [Tầng 3: Infrastructure] ──> [Tầng 4: Presentation/Api]
```

### Tầng 1: Domain (Lõi nghiệp vụ trung tâm)
* Bao gồm: Entities, Value Objects, Domain Exceptions, Domain Events, Enums.
* **Quy tắc vàng:**
  * Hoàn toàn là **Pure C#** (.NET Standard / .NET 8/9).
  * **0% dependency bên thứ ba:** Không phụ thuộc EF Core, không phụ thuộc ASP.NET Core hay bất kỳ thư viện ORM nào.
  * Tọa độ GIS biểu diễn bằng cặp giá trị WGS84 `[lng, lat]` theo chuẩn Vendor-Agnostic (như BRD quy định).

### Tầng 2: Application (Use Cases & Điều phối)
* Bao gồm: Interfaces (IRepositories, IUnitOfWork, ICurrentUserService,...), DTOs, Mappers, Business Validators, Use Case Services/Handlers.
* Chỉ phụ thuộc vào `Domain`.

### Tầng 3: Infrastructure (Hiện thực hóa & Tích hợp)
* Bao gồm: `DbContext`, EF Core Configurations, PostgreSQL + PostGIS (NetTopologySuite), Migration scripts, Redis Cache, AWS S3 Client, Repositories Implementation.
* Phụ thuộc vào `Application` và `Domain`.

### Tầng 4: Presentation / Web API (Cổng giao tiếp)
* Bao gồm: Controllers / Endpoints, Middlewares (Exception Handler, Logging), Filters, Swagger/OpenAPI setup, Dependency Injection registration tại `Program.cs`.
* Phụ thuộc vào `Infrastructure` và `Application`.

> **Quy tắc bất biến:** Nghiêm cấm viết Controller hoặc tạo Database Migration khi Domain Entities và Application Contracts chưa được hoàn thiện và User duyệt.

---

## 5. Tiêu Chuẩn Nghiệm Thu & Kiểm Chứng (Verification & Quality Gate)

Mỗi bước hoàn thành phải đảm bảo:
1. Lệnh `dotnet build` chạy thành công mà không có lỗi biên dịch.
2. Dependency Graph tuân thủ nghiêm ngặt chiều mũi tên: Lớp bên ngoài phụ thuộc lớp bên trong; lớp bên trong tuyệt đối không có reference ngược ra ngoài.
3. Code bám sát trực tiếp các quy tắc nghiệp vụ trong `../tripory/docs`.
