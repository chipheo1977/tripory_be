# C# & .NET Core Concepts & Deep Dives

Tài liệu đúc kết các khái niệm kỹ thuật chuyên sâu về C#, .NET Runtime và Best Practices được áp dụng trong codebase `tripory_be`.

---

## 1. Contravariance trong Generics (`in` Keyword)

* **Codebase reference:** [`ICommandHandler<in TCommand>`], [`ICommandHandler<in TCommand, TResponse>`]
* **Khái niệm:** 
  * `in` biểu thị **Contravariance** (Nghịch biến). Nghĩa là bạn chỉ có thể truyền kiểu dữ liệu vào (nhận qua tham số method), không thể trả về kiểu đó từ interface.
  * Cho phép gán một interface có generic type tổng quát (base type) cho một biến có generic type chuyên biệt (derived type): 
    Nếu `AdminCommand` kế thừa từ `BaseCommand`, thì `ICommandHandler<BaseCommand>` có thể được dùng ở bất kỳ nơi nào cần `ICommandHandler<AdminCommand>`.
* **Tại sao áp dụng trong CQRS:** 
  * Handler chỉ **tiêu thụ (consume)** Command làm đầu vào, không bao giờ sinh hay return command.
  * Giúp MediatR và DI container linh hoạt phân giải Handler mà không bị lỗi ép kiểu Generic bất biến (Invariance).

---

---

## 2. CQRS Marker Interfaces kế thừa từ MediatR

* **Codebase reference:** [`ICommand`], [`ICommand<TResponse>`]
* **Khái niệm:**
  * Thay vì để Application Layer trực tiếp implement `IRequest<Result>` hay `IRequest<Result<T>>` của MediatR, hệ thống định nghĩa interface trừu tượng riêng: `ICommand` và `IQuery`.
* **Lợi ích kiến trúc:**
  1. **Tách biệt rõ rệt Ý định (Intent):** Phân định rõ Command (thay đổi trạng thái - mutation) và Query (đọc dữ liệu - read-only).
  2. **Pipeline Behavior Targeting:** Cho phép viết các MediatR Pipeline Behavior áp dụng riêng cho Command (ví dụ: `TransactionPipelineBehavior`, `ValidationPipelineBehavior`), trong khi Query có thể gắn `CachingPipelineBehavior`.
  3. **Abstraction over Third-Party:** Domain và Application core phụ thuộc vào CQRS contract của hệ thống, giảm sự phụ thuộc cứng vào thư viện bên ngoài.

---

## 3. Generic Type Constraints (`where T : ...`)

* **Codebase reference:** [`ICommandHandler<in TCommand>`], [`ICommandHandler<in TCommand, TResponse>`]
* **Khái niệm:**
  * Mệnh đề `where TCommand : ICommand` hoặc `where TCommand : ICommand<TResponse>` ràng buộc kiểu tham số Generic phải kế thừa hoặc triển khai một type/interface cụ thể.
* **Tại sao áp dụng:**
  * **Compile-time Safety:** Ngăn chặn tuyệt đối việc truyền nhầm một class không hợp lệ (ví dụ: nhầm một `Query` hay một DTO tự do) vào `CommandHandler`.
  * **Ràng buộc tương thích:** Đảm bảo `ICommandHandler<TCommand, TResponse>` chỉ chấp nhận `TCommand` nào có kiểu trả về khớp với `TResponse`, tránh việc đăng ký handler trả về `string` cho một command khai báo trả về `Guid`.

---

## 4. ClaimsPrincipal & Kỹ Thuật Trích Xuất Claims Từ Expired JWT Token

* **Codebase reference:** [`IJwtTokenService.GetPrincipalFromExpiredToken`]
* **Khái niệm:**
  * **`ClaimsPrincipal` (`System.Security.Claims`):** Là đối tượng trừu tượng hóa danh tính người dùng trong .NET. Một `ClaimsPrincipal` sở hữu một hoặc nhiều `ClaimsIdentity`, trong đó mỗi mẩu thông tin (ID, Email, Role...) được lưu trữ dưới dạng một `Claim`.
  * **Vấn đề khi triển khai Refresh Token:** Khi Access Token đã hết hạn (`exp` đã qua), nếu giải mã bằng `JwtSecurityTokenHandler.ValidateToken` với cấu hình mặc định (`ValidateLifetime = true`), CLR / .NET runtime sẽ lập tức ném ra ngoại lệ `SecurityTokenExpiredException`. Điều này khiến server không thể đọc được `UserId` từ payload để đối chiếu với `RefreshToken` lưu trong cơ sở dữ liệu.
  * **Kỹ thuật `ValidateLifetime = false`:** Để đọc được danh tính người dùng từ token đã hết hạn, ta cấu hình `TokenValidationParameters` với `ValidateLifetime = false`.
* **Tại sao phải đọc `UserId` từ payload để đối chiếu với `RefreshToken` trong DB?**
  1. **Ràng buộc chặt chẽ cặp Token với Chủ sở hữu (Cryptographic Binding):** `RefreshToken` là chuỗi ngẫu nhiên (opaque string) không mang thông tin người dùng. Việc đọc `UserId` từ Access Token (đã được server ký bảo mật) giúp server xác định đích danh ai đang xin gia hạn và kiểm tra xem chuỗi `RefreshToken` gửi lên có đúng là của chính User đó hay không.
  2. **Chống tấn công tráo đổi Token (Token Swapping Attack):** Ngăn chặn trường hợp kẻ tấn công có Refresh Token hợp lệ của tài khoản A nhưng lại gửi kèm Access Token đánh cắp được của tài khoản B nhằm chiếm đoạt quyền truy cập của B.
  3. **Kiểm tra trạng thái thời gian thực của tài khoản (Account State Validation):** Khi tra cứu theo `UserId` trong DB, server đồng thời kiểm tra được:
     * Tài khoản có bị khóa (`IsLocked`), xóa mềm, hoặc hạ quyền (`Role`) hay không.
     * Người dùng có vừa đổi mật khẩu hoặc bấm *"Đăng xuất khỏi mọi thiết bị"* hay không (lúc này `RefreshToken` trong DB đã bị vô hiệu hóa).
  4. **Tối ưu hiệu năng truy vấn Database (Index Lookup):** Tra cứu theo `UserId` (Khóa chính / Clustered Index $O(1)$ hoặc B-Tree) luôn nhanh hơn nhiều so với việc quét tìm một chuỗi string `RefreshToken` ngẫu nhiên trong toàn bộ bảng dữ liệu.
* **Cạm bẫy bảo mật tối quan trọng (Security Gotcha):**
  * Tuyệt đối không cho phép bỏ qua chữ ký số, vì kẻ tấn công có thể lợi dụng điều này để làm giả một JWT chứa `UserId` của nạn nhân/Admin mà không cần biết Secret Key của hệ thống.

---

## 5. .NET Service Lifetimes: Transient, Scoped, Singleton

* **Codebase reference:** 
  * [`Persistence/DependencyInjection.cs`]
  * [`Infrastructure/DependencyInjection.cs`]
* **Khái niệm:**
  * `Microsoft.Extensions.DependencyInjection` quản lý vòng đời đối tượng thông qua 3 chế độ cấp phát:
    1. **`Transient` (`AddTransient`):** Tạo mới một instance độc lập mỗi lần có yêu cầu inject. Thích hợp cho các dịch vụ nhẹ, không lưu trữ trạng thái (Stateless) như Validators.
    2. **`Scoped` (`AddScoped`):** Tạo duy nhất 1 instance cho mỗi phạm vi (Scope - thông thường là 1 HTTP Request). Tất cả các class cùng tham gia xử lý 1 request sẽ dùng chung instance này. Khi HTTP request kết thúc, container tự động gọi `Dispose()` để giải phóng tài nguyên.
    3. **`Singleton` (`AddSingleton`):** Tạo duy nhất 1 instance xuyên suốt vòng đời của toàn bộ ứng dụng (từ lúc server bật đến khi tắt).
* **Tại sao áp dụng trong Tripory:**
  * **Tại sao `DbContext`, `UserRepository`, `UnitOfWork` bắt buộc phải là `Scoped`?
    * Để đảm bảo tất cả các thao tác dữ liệu trong cùng một HTTP Request chia sẻ chung một `ChangeTracker` của EF Core, giúp commit dữ liệu nguyên khối (Atomic Transaction) và tự động đóng connection khi request hoàn tất.
  * **Tại sao `BcryptPasswordHasher` và `JwtTokenService` lại là `Singleton`?**
    * Vì chúng là các dịch vụ xử lý thuần túy (Pure computation, Stateless, Thread-safe). Tái sử dụng 1 instance duy nhất giúp tiết kiệm chi phí cấp phát bộ nhớ (Garbage Collector) và CPU.
* **Cạm bẫy vòng đời (Captive Dependency Gotcha):**
  * Tuyệt đối không inject một dịch vụ `Scoped` vào một dịch vụ `Singleton`. Điều này sẽ biến dịch vụ `Scoped` thành `Singleton` ngoài ý muốn (Captive Dependency), gây rò rỉ bộ nhớ (Memory Leak) và lỗi xung đột đa luồng trên DbContext.

---

## 6. Assembly Scanning trong .NET (Reflection-based DI Registration)

* **Codebase reference:** [`Tripory.Application/DependencyInjection.cs`]
* **Khái niệm:**
  * Thay vì phải đăng ký thủ công từng CommandHandler, QueryHandler, hoặc Validator vào `IServiceCollection`, ta sử dụng kỹ thuật quét Assembly (Assembly Scanning) qua Reflection.
  * `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));`
  * `services.AddValidatorsFromAssembly(assembly);`
* **Lợi ích kỹ thuật:**
  * **Tuân thủ Open-Closed Principle (OCP):** Khi lập trình viên tạo thêm UseCase mới (`CreateItineraryCommand`, `CreateItineraryCommandHandler`), hệ thống tự động nhận diện và đăng ký vào DI Container mà không cần mở file cấu hình DI để sửa.
  * **Đăng ký Open Generic Behaviors:** Hỗ trợ đăng ký pipeline xử lý xuyên suốt cho mọi Command/Query bằng cú pháp `cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));`.

---

## 7. EF Core Owned Entity Types (`OwnsOne`): Ánh Xạ Value Object Đa Giá Trị

* **Codebase reference:**
  * [`ChatMessageConfiguration.cs`]
  * [`ChatMessage.cs`]
  * [`CallLogData.cs`]
* **Khái niệm:**
  * **Owned Entity Type** là cơ chế của Entity Framework Core cho phép ánh xạ một đối tượng phụ thuộc (thường là **Value Object**) vào **cùng một bảng** với Entity sở hữu nó (Table Sharing / Inlining).
  * Đối tượng được khai báo qua `OwnsOne` không có khóa chính (`Id`) độc lập trong cơ sở dữ liệu. Vòng đời của nó gắn liền hoàn toàn với Entity cha.
* **So sánh hai phương thức ánh xạ Value Object trong EF Core:**
  1. **`HasConversion` (Dành cho Value Object Đơn giá trị - Single-value VO):**
     * Áp dụng khi Value Object chỉ bọc đúng 1 thuộc tính primitive (ví dụ: `Email` bọc `string`, `Handle` bọc `string`).
     * EF Core chuyển đổi 1-1 đối tượng thành 1 cột primitive duy nhất trong bảng (vd: `email VARCHAR(256)`).
  2. **`OwnsOne` (Dành cho Value Object Đa giá trị / Complex Type):**
     * Áp dụng khi Value Object gồm nhiều thuộc tính thành phần (ví dụ: [`CallLogData`] gồm `Status`, `DurationSeconds`, `Direction`).
     * EF Core "làm phẳng" (flatten) các trường của Value Object thành **nhiều cột riêng biệt trên cùng một bảng** (`call_status`, `call_duration_seconds`, `call_direction`).
* **Tại sao áp dụng trong Tripory:**
  * **Tối ưu hóa hiệu năng I/O:** Không cần tạo thêm bảng riêng `call_logs` và loại bỏ hoàn toàn chi phí `JOIN` bảng đắt đỏ khi truy vấn lịch sử tin nhắn.
  * **Xử lý Nullable tự nhiên:** Khi tin nhắn là dạng Text hoặc Voice (không có cuộc gọi), EF Core tự động gán giá trị `null` cho các cột của `CallLogData`. Khi đọc lên, nếu các cột này là null, property `CallLogData` trên `ChatMessage` sẽ tự động mang giá trị `null`.
  * **Bảo vệ toàn vẹn DDD:** `CallLogData` là một bản ghi dữ liệu bất biến (Value Object), không có bản sắc (Identity) riêng. Khi xóa `ChatMessage`, toàn bộ dữ liệu cuộc gọi tự động biến mất mà không để lại rác dữ liệu mồ côi (Orphan records).
* **Lưu ý kỹ thuật (Gotchas):**
  * Mặc định, nếu không dùng `.HasColumnName(...)`, EF Core sẽ tự sinh tên cột theo công thức `<NavigationProperty>_<TargetProperty>` (ví dụ: `CallLogData_DurationSeconds` dạng PascalCase).
  * Trong dự án `tripory_be`, bắt buộc phải cấu hình tường minh `.HasColumnName("call_duration_seconds")` để tuân thủ quy chuẩn đặt tên `snake_case` thống nhất của PostgreSQL.

---

## 8. EF Core Change Tracking & Kỹ Thuật Tối Ưu Với `AsNoTracking()`

* **Codebase reference:**
  * [`ConversationRepository.cs`] (`GetUserConversationsAsync` vs `GetByIdAsync`)
  * [`ApplicationDbContext.cs`] (`UpdateAuditableEntities`)
* **Khái niệm:**
  * **Cơ chế Snapshot Change Tracking của EF Core:** Khi một Entity được truy vấn từ Database (mặc định), EF Core tạo một bản sao (Snapshot) lưu vào bộ nhớ đệm `ChangeTracker`. Khi `SaveChangesAsync()` được gọi, `ChangeTracker` quét và so sánh từng thuộc tính hiện tại với bản snapshot để phát hiện biến đổi (Detect Changes) và tự động sinh câu lệnh SQL `UPDATE` tương ứng.
  * **`AsNoTracking()`**: Là phương thức mở rộng (Extension Method) chỉ thị cho EF Core bỏ qua hoàn toàn cơ chế chụp ảnh lưu vết. Các thực thể trả về mang trạng thái `Detached` (ngắt kết nối khỏi `DbContext`).
* **Tại sao áp dụng trong Tripory & Liên hệ với CQRS:**
  * **Phía Query (Read-only / Hiển thị UI):** 
    * 100% các câu truy vấn phục vụ đọc dữ liệu (như [`GetUserConversationsAsync`]) bắt buộc sử dụng `.AsNoTracking()`.
    * **Tối ưu RAM:** Loại bỏ hoàn toàn chi phí lưu trữ bản sao snapshot cho hàng chục/hàng trăm bản ghi trên bộ nhớ.
    * **Tối ưu CPU & Tốc độ:** Bỏ qua bước đăng ký vào tracker và thuật toán so sánh snapshot, giúp tốc độ truy vấn nhanh hơn từ **20% đến 50%**.
  * **Phía Command (Ghi / Thay đổi trạng thái nghiệp vụ):**
    * Giữ nguyên cơ chế Tracking mặc định (như `GetByIdAsync`) để khi Handler gọi các method trên Domain Entity (như `conversation.MarkRead()`), EF Core tự động nhận biết thay đổi và cập nhật xuống Database khi gọi `UnitOfWork.SaveChangesAsync()`.
* **Cạm bẫy kỹ thuật (Gotchas):**
  1. **Silent Failure (Lỗi im lặng khi sửa đổi):** Nếu truy vấn một đối tượng với `.AsNoTracking()`, sau đó thay đổi thuộc tính và gọi `SaveChangesAsync()`, EF Core sẽ **hoàn toàn không lưu gì cả** và cũng không ném ngoại lệ (vì ChangeTracker không theo dõi đối tượng đó). Muốn lưu, bắt buộc phải gọi tường minh `_context.Update(entity)`.
  2. **Ảnh hưởng tới Interceptor của DbContext:** Trong [`ApplicationDbContext.cs`], hàm `UpdateAuditableEntities()` can thiệp qua `ChangeTracker.Entries<EntityAuditBase<Guid>>()`. Các entity bị Detached bởi `AsNoTracking()` sẽ không bao giờ xuất hiện trong danh sách duyệt này.





