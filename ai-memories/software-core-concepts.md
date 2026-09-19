# Software Engineering Core Concepts & Principles

Tài liệu đúc kết các nguyên lý thiết kế phần mềm cốt lõi (SOLID, DRY, KISS, YAGNI, GRASP, Design Patterns tổng quát...) được áp dụng trong quá trình xây dựng hệ thống `tripory_be`.

---

## 1. SOLID: Liskov Substitution Principle (LSP - Nguyên lý Thay thế Liskov)

* **Phát biểu nguyên lý:**
  > *"Các đối tượng của lớp con (derived/subtype) phải có thể thay thế hoàn toàn cho các đối tượng của lớp cha (base/supertype) mà không làm thay đổi tính đúng đắn và hành vi mong đợi của chương trình."*
  > — Barbara Liskov

* **Bản chất cốt lõi:**
  1. Lớp con không được phá vỡ các giả định/hợp đồng (contracts & invariants) mà lớp cha hoặc interface đã cam kết với bên gọi (client).
  2. Lớp con không được ném ra exception bất ngờ đối với các method mà lớp cha cho phép chạy bình thường (ví dụ kinh điển: ném `NotImplementedException`).
  3. **Quy tắc điều kiện:**
     * **Contravariance của tham số:** Lớp con không được đòi hỏi điều kiện đầu vào khắt khe hơn lớp cha (Preconditions cannot be strengthened).
     * **Covariance của kết quả:** Lớp con không được trả về kết quả lỏng lẻo hơn hoặc vi phạm cam kết đầu ra của lớp cha (Postconditions cannot be weakened).
     * **Bảo toàn Invariants:** Lớp con phải duy trì mọi bất biến trạng thái của lớp cha.

* **Ví dụ Vi phạm Kinh điển (Anti-pattern):**
  * **Square kế thừa Rectangle:** Class `Square` kế thừa `Rectangle`, khi override `SetWidth(w)` thì tự ý đổi luôn `Height = w`. Client gọi code `rect.SetWidth(5); rect.SetHeight(10); Assert(rect.Area() == 50)` sẽ bị fail khi truyền `Square`.
  * **Throwing Not Supported:** Class `ReadOnlyList` kế thừa `List` nhưng method `Add()` lại `throw new NotSupportedException()`.

* **Ứng dụng thực tế trong kiến trúc Tripory Backend:**
  * **Base Entities & Audit Entities:** [`EntityAuditBase<TKey>`]. Bất kỳ logic nào nhận `EntityBase` đều có thể nhận `EntityAuditBase` mà hành vi kiểm tra `Id`, so sánh equality vẫn giữ nguyên tính đúng đắn.
  * **CQRS Handlers:** [`ICommandHandler<TCommand>`] và [`IQueryHandler<TQuery, TResponse>`]thay thế trực tiếp `IRequestHandler<TCommand, Result>` của MediatR một cách trơn tru, bảo đảm hợp đồng trả về `Result` thống nhất.

---

## 2. Class Invariant (Bất Biến Của Lớp)

* **Định nghĩa cốt lõi:**
  > *"Class Invariant là điều kiện / quy tắc ràng buộc trạng thái mà đối tượng bắt buộc phải thỏa mãn tại mọi thời điểm tồn tại hợp lệ (sau khi khởi tạo và sau mỗi lần thực thi method public)."*
  > $\rightarrow$ **"Không được phá vỡ quy tắc ràng buộc"**

* **Bản chất trong Domain-Driven Design (DDD):**
  * **Encapsulation:** Đối tượng tự bảo vệ invariant của chính nó; không cho phép bên ngoài gán trực tiếp dữ liệu làm trạng thái trở nên không hợp lệ.
  * Mọi biến đổi trạng thái phải đi qua method có kiểm tra invariant (ví dụ: `itinerary.Publish()` kiểm tra xem có ít nhất 1 stop chưa; nếu vi phạm thì từ chối đổi trạng thái).
  * Lớp con khi kế thừa không bao giờ được phép làm suy yếu hoặc phá vỡ các invariants đã định nghĩa ở lớp cha.

---

## 3. Type Variance (Biến Thiên Kiểu Dữ Liệu Trong Hệ Thống Kiểu)

Quy định mối quan hệ kế thừa giữa các kiểu phức hợp (Generic, Delegate) dựa trên mối quan hệ giữa các kiểu con thành phần:

```text
               ┌────────────────────────────────────────────────────────┐
               │                      Type Variance                     │
               └───────┬───────────────────────┬────────────────┬───────┘
                       │                       │                │
                       ▼                       ▼                ▼
                1. Invariance           2. Covariance    3. Contravariance
                (Bất biến)              (Đồng biến)      (Nghịch biến)
                Vừa Đọc vừa Ghi         Chỉ Đọc (Out)    Chỉ Ghi / Nhận xử lý (In)
                Bắt buộc đúng kiểu      Cho phép cụ thể  Cho phép tổng quát hơn
```

### 3.1. Invariance (Bất biến kiểu)
* **Quy tắc:** **Vừa Đọc vừa Ghi $\rightarrow$ bắt buộc phải giữ đúng kiểu tuyệt đối.**
* **Giải thích:** Nếu một cấu trúc dữ liệu cho phép cả ghi dữ liệu vào lẫn đọc dữ liệu ra (ví dụ: `IList<T>`, `List<T>`), kiểu generic bắt buộc phải bất biến.
* **Tại sao:** Nếu cho phép gán `List<Dog>` vào `List<Animal>`, ta có thể ghi một con `Cat` vào danh sách thông qua biến `List<Animal>`, và khi `List<Dog>` đọc ra sẽ nhận phải `Cat` $\rightarrow$ Crash chương trình (phá vỡ type safety tại runtime).

### 3.2. Covariance (Đồng biến kiểu - `out` trong C#)
* **Quy tắc:** **Áp dụng cho đối tượng Chỉ Đọc (Producer / Output) $\rightarrow$ không cần giữ kiểu tuyệt đối.**
* **Giải thích:** Bảo toàn chiều quan hệ kế thừa: Nếu `Dog` là `Animal`, thì `IEnumerable<Dog>` cũng là `IEnumerable<Animal>`.
* **Tại sao an toàn:** Bên nhận chỉ đọc dữ liệu ra. Người cần đọc danh sách các `Animal` thì khi nhận toàn `Dog` đọc ra vẫn hoàn toàn hợp lệ (bởi vì mọi `Dog` đều là `Animal`).

### 3.3. Contravariance (Nghịch biến kiểu - `in` trong C#)
* **Quy tắc:** **Áp dụng cho đối tượng Chỉ Ghi / Tiếp nhận xử lý (Consumer / Input Parameter) $\rightarrow$ Đảo ngược chiều kế thừa.**
* **Giải thích:** Một hàm biết cách xử lý mức tổng quát (`Animal`) thì luôn thừa khả năng xử lý mức cụ thể (`Dog`).
  * Ví dụ: Nếu bạn có một hành động "Khám bệnh cho Animal" (`Action<Animal>`), bạn hoàn toàn có thể dùng nó ở bất kỳ nơi nào cần một hành động "Khám bệnh cho Dog" (`Action<Dog>`).
* **Ứng dụng thực tế trong Tripory CQRS:**
  * [`ICommandHandler<in TCommand>`]: Handler đóng vai trò là Consumer (chỉ nhận command vào để xử lý).
  * [`IDomainEventHandler<in TEvent>`]: Event Handler tiếp nhận event để xử lý. Việc dùng `in` giúp kiến trúc linh hoạt: một handler xử lý sự kiện mức cơ sở có thể tự động xử lý mọi sự kiện con phát sinh.

---

## 4. Phân Định Trách Nhiệm Các Tầng Trong Clean Architecture + CQRS

* **Phát biểu nguyên lý:**
  > Mỗi tầng trong hệ thống chỉ đảm nhận duy nhất một trách nhiệm cốt lõi (Single Responsibility Principle ở cấp độ kiến trúc), dependency tuân thủ nghiêm ngặt chiều hướng tâm từ ngoài vào trong: `API` $\rightarrow$ `Application` $\rightarrow$ `Domain` $\leftarrow$ `Persistence`/`Infrastructure`. Mọi request thay đổi trạng thái (Command) đều trải qua chu trình 6 chặng khép kín.

* **Bản chất cốt lõi (Bảng phân công trách nhiệm từng tầng):**

| Tầng | File tiêu biểu | Trách nhiệm cốt lõi | Điều cấm kỵ (What NOT to do) |
| :--- | :--- | :--- | :--- |
| **API** | [`AuthController.cs`], [`ApiResponse.cs`] | Cửa ngõ HTTP: Tiếp nhận HTTP request, giải mã JSON, ghi log, chuyển lệnh vào MediatR và ánh xạ `Result` sang HTTP Status Code tương ứng. | ❌ Không chứa business logic.<br>❌ Không gọi trực tiếp DbContext hay Repository. |
| **Pipeline** | [`ValidationPipelineBehavior.cs`], [`RegisterCommandValidator.cs`] | Trạm gác tự động: Chặn và kiểm tra tính hợp lệ của dữ liệu đầu vào (FluentValidation) trước khi tiêu tốn tài nguyên chạy nghiệp vụ. Trả về `Result.Failure` ngay nếu sai. | ❌ Không gọi I/O database phức tạp.<br>❌ Không quyết định trạng thái nghiệp vụ. |
| **Application** | [`RegisterCommandHandler.cs`] | Nhạc trưởng điều phối (Orchestration): Nhận command -> gọi Repository đọc dữ liệu -> gọi Domain Entity áp quy tắc -> gọi Repository/UoW lưu trữ -> map DTO trả về. | ❌ Không ôm business rules (`if (status == ...) throw`).<br>❌ Không inject `DbContext` hay `DbSet`. |
| **Domain** | [`User.cs`], [`Email.cs`], [`Handle.cs`] | Trái tim nghiệp vụ (Pure C#): Bảo vệ toàn bộ bất biến (Invariants), quản lý trạng thái, đóng gói danh sách con (`_userRoles`, `_refreshTokens`), chỉ cho phép thay đổi qua method. | ❌ Không phụ thuộc EF Core, Web types, I/O.<br>❌ Không dùng Data Annotations (`[Table]`, `[Column]`). |
| **Infrastructure** | [`BcryptPasswordHasher.cs`], [`JwtTokenService.cs`] | Triển khai các công nghệ kỹ thuật bên ngoài: Băm mật khẩu (BCrypt), phát hành JWT token, sinh chuỗi ngẫu nhiên bảo mật (CSPRNG), gửi mail, gọi external API. | ❌ Không tự tiện quyết định logic nghiệp vụ (phải ủy thác cho Domain). |
| **Persistence** | [`UserRepository.cs`], [`ApplicationDbContext.cs`], [`UnitOfWork.cs`] | Lưu trữ và truy xuất dữ liệu bền vững: Ánh xạ Fluent API với PostgreSQL, quản lý giao dịch (Transaction) qua UnitOfWork để đảm bảo tính toàn vẹn (ACID). | ❌ Không chứa business rules.<br>❌ Không đưa logic tính toán vào Repository. |

* **Ví dụ Vi phạm (Anti-patterns phổ biến):**
  * **Anemic Domain Model (Mô hình thiếu máu):** Viết Entity rỗng chỉ có `{ get; set; }`, sau đó viết toàn bộ logic kiểm tra hợp lệ và đổi trạng thái vào Handler $\rightarrow$ Phá vỡ tính đóng gói của OOP.
  * **Fat Controller:** Viết code băm mật khẩu hoặc query Entity ngay trong Action của Controller $\rightarrow$ Khiến code không thể viết Unit Test và gắn chặt với giao thức HTTP.
  * **Lẫn lộn I/O vào Domain:** Cho Entity trực tiếp gọi `HttpClient` hay `DbContext` $\rightarrow$ Vi phạm nguyên tắc Pure C# của Domain.

* **Ứng dụng thực tế trong Tripory Backend:**
  * Mọi Use Case (từ Auth, User Profile cho đến Lịch trình, Nhắn tin) đều vận hành theo đúng quy chuẩn phân tầng 6 chặng này, giúp hệ thống dễ bảo trì, dễ mở rộng và kiểm thử độc lập 100%.

---

## 5. Inversion of Control (IoC) & Dependency Injection (DI)

* **Phát biểu nguyên lý:**
  * **Inversion of Control (IoC):** Là nguyên lý kiến trúc chuyển giao quyền kiểm soát luồng thực thi và khởi tạo đối tượng từ code ứng dụng cho một Framework hoặc Container bên ngoài.
  * **Dependency Injection (DI):** Là một Design Pattern cụ thể để hiện thực hóa IoC, trong đó các phụ thuộc (Dependencies) được "tiêm" vào đối tượng từ bên ngoài (chủ yếu qua Constructor Injection) thay vì đối tượng tự khởi tạo bằng `new`.
  * **Mối quan hệ:** IoC là tư tưởng/triết lý (Ý niệm) $\rightarrow$ DI là công cụ/kỹ thuật thực thi (Hành động).

* **Bản chất cốt lõi & Lợi ích kiến trúc:**
  1. **Giảm liên kết chặt (Loose Coupling):** Các module cấp cao chỉ giao tiếp qua Interface trừu tượng, không phụ thuộc vào class cụ thể.
  2. **Dễ viết Unit Test (Testability):** Cho phép thay thế implementation thật bằng Mock/Stub (ví dụ: `Mock<IUserRepository>`) mà không sửa một dòng code nào trong Handler.
  3. **Quản lý vòng đời tập trung (Lifecycle Management):** Container tự động theo dõi việc tạo, cấp phát và giải phóng tài nguyên (Dispose) khi hết vòng đời.
  4. **Tuân thủ SOLID:** Thỏa mãn Dependency Inversion Principle (DIP) và Single Responsibility Principle (SRP).

* **Cạm bẫy & Anti-patterns cần tránh:**
  * **Service Locator Anti-pattern:** Inject `IServiceProvider` vào trong class rồi gọi `serviceProvider.GetService<T>()` $\rightarrow$ Che giấu các phụ thuộc thực sự, phá vỡ tính tường minh của Constructor Injection.
  * **Inject Domain Model vào DI Container:** Cố tình đăng ký `User`, `Email`, `Handle`, `RegisterCommand` vào `IServiceCollection`. Domain Entities/Value Objects là **Dữ liệu (Data/State)** có vòng đời gắn với nghiệp vụ, phải được tạo qua `new`, Factory Method (`User.Create`) hoặc JSON deserializer, **tuyệt đối không đăng ký vào DI**.

* **Ứng dụng thực tế trong Tripory Backend:**
  * **Composition Root:** [`Program.cs`] đóng vai trò lắp ráp các module (`AddApplication()`, `AddPersistence()`, `AddInfrastructure()`).
  * **Constructor Injection:** [`RegisterCommandHandler.cs`] nhận `IUserRepository`, `IUnitOfWork`, `IPasswordHasher`, `IJwtTokenService` qua hàm dựng.
  * **DIP Boundary:** Application sở hữu Interface [`IUserRepository.cs`], Persistence triển khai [`UserRepository.cs`]. DI Container gắn kết chúng qua `services.AddScoped<IUserRepository, UserRepository>()`.

---

## 6. Command Query Responsibility Segregation (CQRS)

* **Phát biểu nguyên lý:**
  * **CQRS:** Phân tách rạch ròi giữa các tác vụ thay đổi dữ liệu (**Command**) và các tác vụ đọc dữ liệu (**Query**). Kế thừa từ nguyên lý CQS (Command-Query Separation) của Bertrand Meyer: *"Hỏi một câu hỏi thì không được làm thay đổi câu trả lời"*.
  * **Command (Lệnh):** Thay đổi trạng thái hệ thống (Create, Update, Delete), không trả về dữ liệu lớn mà chỉ trả về trạng thái thành công/thất bại kèm định danh (ID, Token).
  * **Query (Truy vấn):** Đọc dữ liệu (Read-only), trả về DTO hiển thị, tuyệt đối không làm thay đổi trạng thái của hệ thống.

* **Bản chất cốt lõi & Lợi ích:**
  1. **Tối ưu hóa bất đối xứng (Asymmetric Optimization):** Tỉ lệ Đọc thường cao gấp nhiều lần Ghi. Tách rời giúp phía Query tối ưu bằng Cache, DTO phẳng, `AsNoTracking()` mà không sợ ảnh hưởng đến Transaction của phía Command.
  2. **Giải phóng Domain Model:** Phía Command cần Domain Entity giàu logic (Rich Domain) bảo vệ Invariants; phía Query chỉ cần DTO phẳng tối giản cho UI.
  3. **Bảo mật và phân quyền:** Dễ dàng áp dụng quyền hạn riêng biệt (chỉ Admin mới gửi được `BanUserCommand`, Traveler được chạy `GetDiscoveryFeedQuery`).

* **Cạm bẫy & Anti-patterns:**
  * **Lạm dụng Full CQRS + Event Sourcing khi chưa cần thiết:** Tách riêng 2 Database vật lý và đồng bộ bất đồng bộ qua Event Bus cho một hệ thống CRUD nhỏ sẽ gây ra ác mộng về Eventual Consistency. Khuyến nghị bắt đầu từ **Logical CQRS** (tách code trong cùng 1 database).
  * **Query làm thay đổi trạng thái ngầm:** Viết một Query lấy thông tin người dùng nhưng lại tự ý cập nhật trường `LastLoginAt` vào DB $\rightarrow$ Vi phạm CQS nghiêm trọng.

* **Ứng dụng thực tế trong Tripory Backend:**
  * **Marker Interfaces:** [`ICommand`], [`ICommand<TResponse>`] so với [`IQuery<TResponse>`] tại `BuildingBlocks.Core.CQRS`.
  * **Feature Vertical Slice:** Tách biệt thư mục `Commands/` và `Queries/` trong `Tripory.Application/UseCases/V1/`.
  * **Tách biệt Handler:** [`RegisterCommandHandler.cs`] (Command: gọi `User.Create`, băm mật khẩu, mở Transaction lưu DB) đối trọng với [`GetCurrentUserProfileQueryHandler.cs`] (Query: chỉ đọc DTO, không bao giờ gọi `SaveChangesAsync`).

---

## 7. Domain-Driven Design (DDD) - Rich Domain Model & Tactical Patterns

* **Phát biểu nguyên lý:**
  * Phương pháp luận phát triển phần mềm đặt **trọng tâm cốt lõi vào Miền nghiệp vụ (Domain)** và **Quy tắc kinh doanh (Business Invariants)**. Mọi logic cốt lõi phải được viết bằng Pure C#, độc lập 100% với Database, Web Framework hay I/O.

* **Bản chất cốt lõi (Các khối chiến thuật Tactical Patterns):**
  1. **Entity:** Đối tượng có danh tính duy nhất (`Id`), dữ liệu thay đổi theo thời gian nhưng bản sắc (identity) không đổi (ví dụ: [`User.cs`]).
  2. **Value Object:** Đối tượng không có `Id`, bất biến (Immutable), đại diện cho một khái niệm đo lường/mô tả và so sánh bằng giá trị thành phần (Structural Equality) (ví dụ: [`Email.cs`], [`Handle.cs`]).
  3. **Aggregate & Aggregate Root:**
     * Là một cụm các Entity và Value Object có liên kết chặt chẽ, được bao bọc bởi một **Aggregate Root** (Thực thể đầu đàn, implement [`IAggregateRoot`]).
     * **Quy tắc ranh giới (Boundary Rule):** Mọi tương tác từ bên ngoài vào các thực thể con bên trong đều bắt buộc phải thông qua Aggregate Root. Các thực thể con (như [`RefreshToken.cs`]) ẩn constructor và method dưới phạm vi `internal`.
  4. **Quy tắc Repository của DDD:**
     * **Chỉ có Aggregate Root mới được phép có Repository.**
     * Không tạo `RefreshTokenRepository` hay `UserRoleRepository`. Mọi thao tác lưu/xóa token đều thực hiện thông qua `IUserRepository` trên Aggregate `User`.

* **Cạm bẫy & Anti-patterns:**
  * **Anemic Domain Model (Mô hình thiếu máu):** Viết Entity chỉ toàn `{ get; set; }`, đẩy toàn bộ logic kiểm tra và đổi trạng thái ra Service/Handler.
  * **Tạo Repository tràn lan cho mọi bảng:** Coi Repository như một lớp Data Access Layer thông thường cho từng bảng trong database thay vì coi nó là kho lưu trữ cho cả một Aggregate.

* **Ứng dụng thực tế trong Tripory Backend:**
  * **Rich Domain:** [`User.cs`] giấu private constructor, dùng Factory Method `User.Create(...)` kiểm tra invariants, phương thức `ChangePassword(...)` tự động kích hoạt cascading revocation trên toàn bộ Refresh Tokens.
  * **Boundary:** Collection `_refreshTokens` là `private readonly List<RefreshToken>`, bên ngoài chỉ được xem qua `IReadOnlyCollection<RefreshToken>`.
  * **Single Repository:** Chỉ có [`IUserRepository.cs`], không có repository lẻ cho các bảng nối hay bảng phụ.


