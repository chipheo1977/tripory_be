# Architectural Decisions & Design Patterns

Ghi nhận các quyết định kiến trúc, mẫu thiết kế và chuẩn công nghệ của `tripory_be`.

---

## Danh Sách Quyết Định

### [ADR-001] Cơ Chế Xác Thực Kép (Dual-Token Authentication: Short-lived Access Token & Long-lived Refresh Token)
* **Ngày ghi nhận:** 2026-09-17
* **Bối cảnh (Context):** 
  * Hệ thống Web/Mobile API cần xác thực an toàn, stateless để đảm bảo khả năng mở rộng (scalability), nhưng đồng thời phải bảo đảm trải nghiệm người dùng (UX) không bị ngắt quãng.
  * Nếu dùng Access Token dài hạn: Kẻ tấn công nếu đánh cắp được token (qua XSS hoặc bắt gói tin) có thể giả mạo nạn nhân trong thời gian dài mà server không thể thu hồi (revoke) ngay lập tức vì JWT là stateless.
  * Nếu dùng Access Token ngắn hạn đơn lẻ: Người dùng sẽ liên tục bị văng phiên đăng nhập (logout) sau mỗi vài phút, gây ức chế trải nghiệm.
* **Quyết định (Decision):** Áp dụng mô hình **Dual-Token Authentication**:
  1. **Access Token (Stateless JWT):**
     * Thời hạn ngắn (15 - 30 phút).
     * Chứa các claims nhận diện cơ bản (`UserId`, `Email`, `Role`) để API Gateway và Middlewares xác thực trực tiếp mà không cần truy vấn cơ sở dữ liệu ở mỗi HTTP request.
  2. **Refresh Token (Opaque/Cryptographic String):**
     * Thời hạn dài (7 - 30 ngày), được băm hoặc lưu an toàn trong cơ sở dữ liệu gắn với từng User.
     * Chỉ dùng tại endpoint `/api/v1/auth/refresh-token` để xin cấp lại cặp token mới khi Access Token hết hạn.
     * Cho phép thu hồi phiên làm việc (Revocation) từ phía server (đánh dấu vô hiệu hóa hoặc xóa trong DB khi người dùng đăng xuất hoặc đổi mật khẩu).
* **Đánh đổi (Trade-offs):**
  * *Ưu điểm:* Cân bằng tối ưu giữa tính bảo mật (thu hẹp khung thời gian rủi ro khi lộ Access Token) và trải nghiệm người dùng (tự động làm mới ngầm); cho phép kiểm soát việc đăng xuất/thu hồi quyền từ server.
  * *Nhược điểm:* Tốn thêm tài nguyên I/O cơ sở dữ liệu khi thực hiện cấp mới token; đòi hỏi xử lý thêm cơ chế chống lạm dụng như xoay vòng token (Refresh Token Rotation).
* **Files liên quan:**
  * [`IJwtTokenService.cs`]
  * [`User.cs`]

---

### [ADR-002] Tách Biệt Lệnh & Truy Vấn Ở Cấp Độ Code (Logical CQRS với MediatR)
* **Ngày ghi nhận:** 2026-09-19
* **Bối cảnh (Context):** 
  * Dự án cần phân tách rõ ràng giữa các hành vi thay đổi trạng thái (Ghi) và các hành vi đọc dữ liệu (Đọc) để tối ưu hiệu năng, bảo mật và khả năng bảo trì.
  * Việc áp dụng Full CQRS (2 Database riêng biệt cho Đọc và Ghi kết hợp Event Sourcing) ở giai đoạn khởi đầu dự án là quá phức tạp (Over-engineering), tốn kém chi phí vận hành và rủi ro bất đồng bộ dữ liệu (Eventual Consistency).
* **Quyết định (Decision):** 
  * Áp dụng **Logical CQRS (CQRS mức logic code)**:
    1. Định nghĩa các Marker Interfaces trừu tượng hóa MediatR: `ICommand`, `ICommand<TResponse>` và `IQuery<TResponse>`.
    2. Tổ chức UseCases theo Feature Vertical Slice: tách hẳn thư mục `Commands/` và `Queries/`.
    3. Phía Command được phép mở Transaction, gọi Domain Entity áp rules và gọi `_unitOfWork.SaveChangesAsync`.
    4. Phía Query chỉ đọc dữ liệu, map trực tiếp sang DTO phẳng, không bao giờ gọi `SaveChanges` hay làm đổi trạng thái DB.
    5. Cả hai luồng tạm thời chia sẻ chung 1 cơ sở dữ liệu PostgreSQL.
* **Đánh đổi (Trade-offs):**
  * *Ưu điểm:* Cấu trúc code sạch sẽ, rõ ràng ý định (Intent), dễ mở rộng thêm Caching behavior cho Query hoặc Transaction behavior cho Command; sẵn sàng tách Database vật lý sau này nếu tải đọc tăng đột biến mà không cần viết lại Application layer.
  * *Nhược điểm:* Tăng số lượng file (mỗi thao tác cần 1 Command/Query, 1 Handler, 1 Response DTO).
* **Files liên quan:**
  * [`ICommand.cs`]
  * [`IQuery.cs`]
  * [`RegisterCommandHandler.cs`]
  * [`GetCurrentUserProfileQueryHandler.cs`]

---

### [ADR-003] Ranh Giới Aggregate Root & Quy Tắc Cấp Hạt Của Repository Trong DDD
* **Ngày ghi nhận:** 2026-09-19
* **Bối cảnh (Context):** 
  * Một đối tượng nghiệp vụ thường gồm nhiều bảng quan hệ (vd: `User` có nhiều `UserRole` và nhiều `RefreshToken`).
  * Nếu tạo Repository cho từng bảng nhỏ (`RefreshTokenRepository`, `UserRoleRepository`), code bên ngoài có thể tự do thêm/sửa/xóa các bản ghi con mà bỏ qua các quy tắc nghiệp vụ bất biến của User (vd: thêm token mà không kiểm tra User bị ban, hoặc xóa token mà không theo dõi chuỗi rotation).
* **Quyết định (Decision):** 
  * Áp dụng nghiêm ngặt quy tắc **Aggregate Root Boundary** của DDD:
    1. Chỉ có Aggregate Root (`User`) mới được phép có Repository ([`IUserRepository.cs`]).
    2. Các thực thể phụ thuộc (`RefreshToken`, `UserRole`) được đóng gói hoàn toàn bên trong Aggregate: ẩn constructor và methods dưới mức truy cập `internal`.
    3. Mọi thao tác thêm, thu hồi token hay gán quyền đều phải được thực thi thông qua phương thức nghiệp vụ trên `User`.
* **Đánh đổi (Trade-offs):**
  * *Ưu điểm:* Bảo toàn 100% tính toàn vẹn dữ liệu và invariants trong cùng 1 Transaction; ngăn chặn hoàn toàn việc can thiệp rác vào database từ các tầng ngoài.
  * *Nhược điểm:* Khi cần cập nhật 1 token con, hệ thống phải nạp cả Aggregate Root `User` lên bộ nhớ (Change Tracker), đòi hỏi cấu hình EF Core kỹ lưỡng (Backing fields).
* **Files liên quan:**
  * [`User.cs`]
  * [`RefreshToken.cs`]
  * [`IUserRepository.cs`]



