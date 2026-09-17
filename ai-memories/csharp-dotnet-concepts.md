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
  * Khi tắt kiểm tra hạn sử dụng (`ValidateLifetime = false`), **BẮT BUỘC** phải duy trì kiểm tra tính toàn vẹn của chữ ký: `ValidateIssuerSigningKey = true` cùng thuật toán ký gốc (`SecurityAlgorithms.HmacSha256`).
  * Tuyệt đối không cho phép bỏ qua chữ ký số, vì kẻ tấn công có thể lợi dụng điều này để làm giả một JWT chứa `UserId` của nạn nhân/Admin mà không cần biết Secret Key của hệ thống.


