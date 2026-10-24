---
title: Logout thu hồi 1 refresh token, CORS theo cấu hình, thời hạn token theo JwtOptions
description: Vì sao logout phải idempotent và chỉ chạm token của chính user, vì sao không được kết hợp AllowCredentials với wildcard origin, và cách để thời hạn refresh token đi theo cấu hình thay vì hard-code
projects: [tripory-be]
type: knowledge
created: 2026-10-17
tags: [extra-knowledge, auth, logout, cors, signalr, options-pattern, jwt]
---

# Logout, CORS, Thời Hạn Token (Nhóm A tích hợp BE–FE)

## Tóm tắt
- `POST /api/v1/auth/logout` thu hồi đúng một refresh token của user đang đăng nhập. Token lạ, đã thu hồi, hết hạn hay của người khác đều trả cùng một response thành công.
- CORS đọc từ `Cors:AllowedOrigins`. Có danh sách thì dùng `WithOrigins` + `AllowCredentials`; rỗng thì giữ hành vi mở như cũ và log WARNING ngoài Development.
- Refresh token và `expiresAt` dùng `JwtOptions:RefreshTokenExpiryDays` (mặc định 7) qua `IJwtTokenService.GetRefreshTokenExpiresAt()`.

## Logout
- **Quyền sở hữu nằm ở cách tìm token:** handler nạp `User` theo `ICurrentUserService.UserId` rồi chỉ tìm trong `user.RefreshTokens`. Token của người khác không bao giờ nằm trong tập này nên không cần (và không được) tra theo chuỗi token toàn hệ thống.
- **Idempotent để không lộ thông tin:** nếu trả 400/404 khi token không tồn tại thì kẻ có access token hợp lệ có thể dò xem một chuỗi token có tồn tại hay không. Trả cùng một envelope thành công cho mọi trường hợp.
- **Không dùng `RevokeAllRefreshTokens`:** logout một thiết bị không được đăng xuất thiết bị khác.
- **Bẫy cần nhớ:** `RefreshTokenCommandHandler` coi token đã thu hồi mà đem refresh là "tái sử dụng" (`Token.Compromised`) và thu hồi mọi phiên. Vì vậy logout xong client phải xóa token, đừng gọi refresh lại bằng token đó.
- Không cần migration: dùng lại `RevokedAt` của `RefreshToken`. Không log chuỗi token.

## CORS và SignalR
- Trình duyệt cấm `Access-Control-Allow-Origin: *` đi cùng credentials, và `SetIsOriginAllowed(_ => true)` thực chất phản chiếu mọi origin nên hiệu lực tương đương wildcard: website bất kỳ đều gọi được API bằng cookie của user. Có danh sách origin thì dùng `WithOrigins`.
- Origin sai định dạng (`*`, thiếu scheme, có path) làm app không khởi động, để lỗi cấu hình lộ ra ngay thay vì âm thầm chặn/mở nhầm.
- SignalR negotiate là request HTTP thường nên chịu CORS; WebSocket thì trình duyệt không áp CORS. Policy vẫn đặt trước `MapHub`.

## Thời hạn token
- Application không được tham chiếu Infrastructure, nên handler không đọc `JwtOptions` trực tiếp mà hỏi `IJwtTokenService`.
- `JwtOptions.ExpiryMinutes` mặc định trong code là 15 nhưng `appsettings.json` đặt 60; thiếu config sẽ im lặng rơi về 15. Hai giá trị đều được kiểm tra `> 0` bằng `ValidateOnStart`.

## Related Files
- `Tripory.Application/UseCases/V1/Auth/Handlers/LogoutCommandHandler.cs`
- `Tripory.API/Cors/CorsExtensions.cs`
- `Tripory.Infrastructure/DependencyInjection.cs`, `Implementations/Security/JwtTokenService.cs`
