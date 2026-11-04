# Luật viết code Tripory BE

Áp dụng cho mọi code mới và code sửa. Kiến trúc: `AGENTS.md` (§4 phân tầng, §5 pattern, §6 điều cấm), `ARCHITECTURE.md`, `ai-memories/architectural-decisions.md`. Cách reviewer chấm các luật này nằm ở `code-review.md`.

## Không bao giờ
- Ghi token (access/refresh), mật khẩu, `SecretKey`, `BffSharedSecret` vào log, URL hoặc response (trừ `AuthResponse`/`TokenResponse`).
- Commit giá trị bí mật vào `appsettings*.json` (`JwtOptions:SecretKey`, `BffSharedSecret`, mật khẩu DB). Dùng biến môi trường dạng `Section__Key`. Trong tài liệu chỉ ghi tên key.
- Đặt cookie từ BE. Token trả trong JSON body, cookie `HttpOnly` do BFF của FE giữ.
- Kết hợp `AllowCredentials` với wildcard origin hoặc `SetIsOriginAllowed(_ => true)` trong code mới. Origin lấy từ `Cors:AllowedOrigins`.
- Bật `EnableSensitiveDataLogging` hoặc `EnableDetailedErrors` ngoài môi trường Development.
- Viết business rule (`if status == X` rồi trả lỗi) trong Handler hoặc Infrastructure. Đưa vào method của Entity/ValueObject.
- Import `Microsoft.EntityFrameworkCore`, `HttpClient`, `DataAnnotations` vào `Tripory.Domain` hoặc `Tripory.Application`. Không inject `DbContext`/`DbSet` vào Handler. Application không tham chiếu Persistence/Infrastructure.
- Dùng `HttpPatch`. Mutation dùng PUT (hoặc POST/DELETE đã có chuẩn).
- Throw exception cho lỗi nghiệp vụ dự kiến (không tìm thấy, không có quyền, sai trạng thái). Trả `Result.Failure(DomainErrors.X)`.
- Tạo `new Error("Mã.Lỗi", "...")` rải rác. Thêm vào `Tripory.Domain/Errors/DomainErrors.cs`.
- Đặt mã lỗi chứa `NotFound`, `Conflict`, `AlreadyExists`, `Unauthorized`, `Forbidden` nếu không muốn nó thành 404/409/401/403. `ApiController.HandlerFailure` ánh xạ theo các chuỗi con này.
- Sửa tay migration đã tạo hoặc `ApplicationDbContextModelSnapshot.cs`. Sai thì tạo migration mới, hoặc `dotnet ef migrations remove` nếu chưa apply.
- Gọi `.Result`, `.Wait()`, `GetAwaiter().GetResult()` trong code async.
- Dùng `DateTime.Now`. Thời gian luôn UTC (`DateTimeOffset.UtcNow`).

## Luôn làm
### Controller / API
- Kế thừa `ApiController`. Action nhận `CancellationToken ct` và truyền xuống `Sender.Send(..., ct)`.
- Dòng đầu action ghi `_logger.LogInformation` bằng tiếng Việt, không chứa token hay mật khẩu.
- Sau `Send`: `if (result.IsFailure) return HandlerFailure(result);` rồi mới `Ok`/`Created`.
- Bind body bằng Request contract riêng ở `Contracts/V1/<Feature>/Requests`, rồi map sang Command. Không bind Command trực tiếp.
- Trả `ApiResponse<T>` hoặc `ApiResponse.Success(message)`. Thêm `[ProducesResponseType]` cho 200/201/400/401/403/404 phù hợp.
- Endpoint ẩn danh ghi dữ liệu hoặc kiểm mật khẩu phải có `[EnableRateLimiting(RateLimitPolicies.X)]`. Thêm `[AllowAnonymous]` mới thì nêu lý do trong PR.

### Application
- Handler chỉ điều phối: đọc, gọi Entity/Domain Service, lưu bằng `IUnitOfWork.SaveChangesAsync(ct)`, map DTO, trả `Result`. Một file một handler.
- Thao tác ghi trên Itinerary lấy aggregate qua `GetOwnedItineraryAsync(id, currentUserId, includeDetails, ct)` để kiểm xác thực, tồn tại và quyền sở hữu.
- Mọi `RuleFor` có `WithMessage` tiếng Việt và `WithErrorCode("Feature.PascalReason")`. Sắp rule theo thứ tự khối form vì chỉ lỗi đầu tiên được trả. Đo độ dài chuỗi sau `Trim`, giống ValueObject.
- Trường đếm hoặc DTO lồng nhau chỉ trả khi truy vấn đã `Include` hoặc đếm bằng subquery.
- Danh sách phân trang: validator ép `PageIndex >= 1`, `PageSize` từ 1 đến 100, trả `PagedResult<T>`.

### Domain / Persistence
- Invariant nằm trong Entity/ValueObject (factory trả `Result`, setter private). Giá trị có luật format lặp lại thì tạo ValueObject.
- Entity đổi trạng thái thì cập nhật `UpdatedAt = DateTimeOffset.UtcNow`.
- Mapping chỉ bằng Fluent API trong `Tripory.Persistence/Configurations/<Entity>Configuration.cs`.
- Port đặt ở `Domain/Abstractions` hoặc `Application/Abstractions`, adapter ở `Infrastructure/Implementations`.

### Cấu hình
- Cấu hình mới dùng Options Pattern: class có `const SectionName`, thuộc tính `init`, mặc định an toàn, `.Validate(...).ValidateOnStart()`. Mỗi option khai báo phải được dùng.

### Migration
- Tạo bằng `dotnet ef migrations add <Tên_gạch_dưới> --project src/Services/Tripory/Tripory.Persistence --startup-project src/Services/Tripory/Tripory.API`.
- Migration chuyển dữ liệu cũ phải thêm SQL tay. Ghi trong PR bước sao lưu DB và cách quay lui. BE không tự Migrate lúc khởi động.
- Chạy thử trên DB phát triển (`docker compose up -d`) trước khi báo xong.

## Hợp đồng với FE
Không đổi nếu chưa cập nhật FE và `API_ENDPOINTS.md` trong cùng thay đổi.
- Envelope `ApiResponse`: `status`, `data`, `message`, `errorCode`, `timestamp` (camelCase).
- `AuthResponse` = `accessToken`, `refreshToken`, `expiresAt`, `user`. `TokenResponse` = `accessToken`, `refreshToken`, `expiresAt`.
- `errorCode` là chuỗi FE ánh xạ ra từng ô form (`ItineraryTitle.*`, `Itinerary.*`, `Token.*`, `RateLimit.TooManyRequests`). Không đổi tên hay xóa mã đã phát hành.
- Refresh token xoay vòng. Dùng lại token đã thu hồi thì thu hồi mọi phiên (`Token.Compromised`). Logout thu hồi đúng 1 token, idempotent, không lộ token của ai.
- `JwtOptions:ExpiryMinutes` không đặt dưới 3 phút (FE làm mới trước hạn 120 giây).
- `PUT /itineraries/{id}`: `destinationCode`/`interests` null = giữ nguyên. `coverImages` null = giữ, `[]` = xóa hết.
- Mã điểm đến (34 slug) và mã sở thích (7) bất biến, khớp từng ký tự với bản sao ở FE.
- Rate limit auth giữ policy `auth-login` 5/60s, `auth-register` 3/3600s, `auth-refresh-token` 20/60s. Trả 429 kèm `Retry-After` và `errorCode` `RateLimit.TooManyRequests`.
- Đổi endpoint, body, mã lỗi hoặc nghĩa của null thì cập nhật `API_ENDPOINTS.md` trong cùng commit và báo cho FE.

## Trước khi báo xong
- Chạy `dotnet build` ở thư mục gốc (`Tripory.slnx`). Repo chưa có project test.
