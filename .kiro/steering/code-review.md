---
inclusion: auto
name: code-review
description: Luật review code Tripory BE (.NET). Dùng khi review code, chạy gsd-code-review hoặc kiểm tra thay đổi trước khi ship.
---

# Luật review code Tripory BE

Kiến trúc: `AGENTS.md` (§4, §5, §6), `ARCHITECTURE.md`.

File này dành cho người review: nó quy định mức độ nghiêm trọng và cách nhận ra vi phạm. Luật viết code mà agent phải tuân theo nằm ở `coding-rules.md`. Các nhãn Critical/Warning, ngưỡng và cách báo ở đây không tạo thêm ràng buộc nào khi viết code.

## Phải gắn nhãn Critical nếu vi phạm
- Bí mật nằm trong code hoặc file cấu hình được commit (`SecretKey`, `BffSharedSecret`, mật khẩu DB), hoặc token/mật khẩu bị ghi vào log, URL, response. Cách tìm: diff của `appsettings*.json`; grep `LogInformation|LogWarning|LogError` kèm `Token|Password|Secret`.
- Endpoint ghi dữ liệu hoặc trả dữ liệu cá nhân mà thiếu `[Authorize]`, hoặc thêm `[AllowAnonymous]` trả email/trạng thái/vai trò của người khác. Cách tìm: đối chiếu thuộc tính ở cấp class và action của controller mới/sửa.
- Handler ghi trên aggregate của người dùng mà không kiểm quyền sở hữu (không qua `GetOwnedItineraryAsync`, hoặc không so `UserId` với `ICurrentUserService.UserId`).
- CORS: `AllowCredentials` đi cùng wildcard hoặc `SetIsOriginAllowed(_ => true)` trong code mới.
- Đổi hợp đồng FE mà không cập nhật `API_ENDPOINTS.md` và báo FE: đổi tên hoặc xóa `errorCode` đã phát hành, đổi tên field JSON, đổi nghĩa null của PUT, đổi slug/mã danh mục, đổi hình dạng `AuthResponse`/`TokenResponse`, đặt cookie ở BE.
- Import xuyên tầng: Domain/Application dùng EF Core, `HttpClient`, `DataAnnotations`; Application tham chiếu Persistence/Infrastructure; thêm `ProjectReference` trái chiều (xem `AGENTS.md` §7).
- Migration phá dữ liệu (`DropColumn`, `DropTable`, `AlterColumn` thu hẹp kiểu) mà PR không ghi bước sao lưu/chuyển dữ liệu; hoặc sửa tay migration/`ApplicationDbContextModelSnapshot.cs` đã commit.
- `EnableSensitiveDataLogging` hoặc `EnableDetailedErrors` chạy ở Production.

## Gắn Warning
- Business rule nằm trong Handler/Infrastructure Service (ví dụ `if (x.Status == ...) return Failure`) thay vì method Entity/ValueObject.
- Throw exception cho lỗi nghiệp vụ dự kiến; hoặc `new Error("...")` inline thay vì `DomainErrors`.
- Validator có `RuleFor` thiếu `WithErrorCode` hoặc `WithMessage` tiếng Việt; rule không theo thứ tự khối form; đo độ dài trước `Trim`.
- Mã lỗi mới chứa `NotFound`/`Conflict`/`AlreadyExists`/`Unauthorized`/`Forbidden` nhưng không định map sang 404/409/401/403.
- Controller: thiếu `CancellationToken`, thiếu `LogInformation` ở đầu action, thiếu `if (result.IsFailure) return HandlerFailure(result)`, thiếu `[ProducesResponseType]`, bind Command trực tiếp thay vì Request contract, dùng `HttpPatch`.
- Handler/Repository bỏ `ct` khi API cho phép truyền (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`...). Ngoại lệ: `IRepositoryBase.UpdateAsync(entity)` chưa có tham số `ct`, không báo.
- Gọi `.Result`/`.Wait()`/`GetAwaiter().GetResult()` trong code async.
- Dùng `DateTime.Now` hoặc thời gian không UTC.
- Cấu hình mới không qua Options Pattern, thiếu `ValidateOnStart`, dùng `set` thay `init`, hoặc option khai báo nhưng không được dùng.
- Truy vấn danh sách không phân trang hoặc không giới hạn `PageSize`; DTO có trường đếm nhưng truy vấn không `Include`/đếm.
- Endpoint ẩn danh mới ghi dữ liệu hoặc kiểm mật khẩu nhưng không có `[EnableRateLimiting]`.
- Message trả cho client viết bằng tiếng Anh, hoặc lộ chi tiết nội bộ (stack trace, tên bảng, SQL).
- Ghi email người dùng vào log (`LogInformation(... {Email})`).
- Thêm/đổi endpoint mà `API_ENDPOINTS.md` không đổi trong cùng thay đổi.

## Ngưỡng gợi ý
Vượt ngưỡng nhưng vẫn mạch lạc thì chỉ ghi Info.
- Handler dài hơn khoảng 80 dòng hoặc constructor nhận hơn 5 phụ thuộc: đề xuất đưa logic xuống Domain.
- Action controller dài hơn khoảng 25 dòng (không tính attribute): có logic không thuộc tầng API.
- Class dài hơn khoảng 300 dòng: cân nhắc tách theo nhóm tài nguyên.

## Bỏ qua
- Style thuần (khoảng trắng, thứ tự `using`, đặt tên biến cục bộ). Repo chưa có formatter/analyzer, nhưng không báo để tránh nhiễu.
- File sinh tự động: `Tripory.Persistence/Migrations/*.cs`, `*.Designer.cs`, `ApplicationDbContextModelSnapshot.cs` (chỉ xét luật "migration phá dữ liệu" ở trên), `bin/`, `obj/`.
- Thư mục công cụ: `.claude/`, `.agents/`, `ai-memories/`, `roadmaps/`.
- Nợ kỹ thuật đã được ghi nhận ở mục cleanup của PR/phase: không báo lại từng lần.
