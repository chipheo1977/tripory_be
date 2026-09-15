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

