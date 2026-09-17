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


