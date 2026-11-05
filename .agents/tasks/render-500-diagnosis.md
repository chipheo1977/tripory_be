# Chẩn đoán HTTP 500 trên Render (register / login)

## Kết luận ngắn

Nguyên nhân gần như chắc chắn: biến `ConnectionStrings__DefaultConnection` trên Render đang chứa **dấu nháy kép bao quanh** (copy nguyên dòng từ `.env.local`, nơi giá trị là `"postgresql://...?channel_binding=require&sslmode=require"`). Chuỗi bắt đầu bằng `"` nên `NpgsqlConnectionStringNormalizer.Normalize` không nhận ra là URL, trả nguyên chuỗi. Npgsql sau đó parse theo dạng key=value và ném `ArgumentException: Format of the initialization string does not conform to specification starting at index 0`. Lỗi xảy ra lười (lúc DbContext được resolve ở request đầu), nên app vẫn khởi động bình thường và mọi endpoint chạm DB đều trả 500 `Server.Error`.

Tôi tái hiện đúng lỗi này ở local. Với cùng giá trị nhưng đã bỏ nháy, register thành công.

Sửa tối thiểu (không cần đổi code): trong Render, sửa `ConnectionStrings__DefaultConnection` thành chuỗi **không có nháy**, bắt đầu bằng `postgresql://`, không có khoảng trắng đầu/cuối, không có tiền tố `psql '`. Redeploy/restart.

Bằng chứng trực tiếp trên Render không đọc được (không có log Render). Đây là suy luận từ tái hiện local; xác nhận bằng cách xem log Render sau khi sửa, hoặc tìm dòng `Format of the initialization string` trong log hiện tại.

## Bằng chứng

### 1. Production DB đủ điều kiện (đọc bằng Npgsql tạm, kết nối UNPOOLED, branch production, database `neondb`)
- `__EFMigrationsHistory` có đủ 5 migration: `20260918103036_Initial_Identity_Tables`, `20260922094520_Add_Chat_Module_Tables`, `20261005085513_Add_Itinerary_PostGIS_Tables`, `20261103085252_Add_Itinerary_Destination_Interests_CoverImages`, `20261104034058_Clean_Itinerary_Cover_Images`.
- Schema `identity` có `users`, `refresh_tokens`, `roles`, `user_roles`, `conversations`, `chat_messages`.
- Extension `postgis 3.6.4` đã cài.
=> Loại trừ: thiếu schema, thiếu extension, sai branch.

### 2. Chạy API local ở Production với URL pooled đã bỏ nháy: thành công
`ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__DefaultConnection` = `DATABASE_URL` (pooled, `-pooler`, `channel_binding=require&sslmode=require`), secret giả.
- `POST /api/v1/auth/register` chạy `SELECT EXISTS ... identity.users` rồi `INSERT identity.users / refresh_tokens / user_roles` thành công qua PgBouncer.
- Lần gửi lặp lại cho kết quả 409 `User.EmailAlreadyExists` (đúng nghiệp vụ).
=> Loại trừ: Npgsql từ chối `channel_binding`, PgBouncer/prepared statements, cấu hình khởi động (JWT, BFF secret, CORS), rate limiter.

### 3. Chạy lại với giá trị đúng nguyên văn `.env.local` (giữ nháy): tái hiện 500
- Response `500`, log `Tripory.API.Middlewares.GlobalExceptionHandler`:
  `System.ArgumentException: Format of the initialization string does not conform to specification starting at index 0.`
  Stack: `NpgsqlConnectionStringBuilder..ctor` ← `NpgsqlDataSourceBuilder..ctor` ← `NpgsqlDataSourceManager.CreateDataSource`.
- Mã liên quan: `src/Services/Tripory/Tripory.Persistence/NpgsqlConnectionStringNormalizer.cs` dòng 13-18 (`StartsWith("postgresql://")` / `"postgres://"` chỉ khớp khi không có nháy đầu); gọi ở `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs` dòng 20, và `UseNpgsql` dòng 31 chạy lười trong lambda `AddDbContext`.
- `.env.local` thực tế: giá trị bắt đầu bằng `"postgresql://` (nháy kép), độ dài 154, query `channel_binding=require&sslmode=require"`.

### 4. Trạng thái dịch vụ live
- `POST https://tripory-be.onrender.com/api/v1/auth/login` trả 500, header `x-render-origin-server: Kestrel` (đến được app). `GET /health` trả 404 (không có endpoint health, không liên quan).

## Khuyến nghị

1. (Bắt buộc, đổi env) Sửa `ConnectionStrings__DefaultConnection` trên Render, bỏ nháy. Dán đúng một dòng URL, ví dụ dạng `postgresql://USER:PASS@HOST-pooler.../neondb?channel_binding=require&sslmode=require`. Render lưu giá trị nguyên văn nên nháy trong ô giá trị bị giữ lại. Có thể dùng pooled URL; đã chạy tốt qua pooler.
2. (Tùy chọn, hardening) Cho `NpgsqlConnectionStringNormalizer.Normalize` `Trim()` khoảng trắng và nháy `"`/`'` đầu cuối trước khi kiểm tra tiền tố, để lỗi dán cấu hình không gây 500 âm thầm. Sửa này chưa áp dụng. Nếu làm, theo `AGENTS.md` cần User duyệt trước.
3. (Tùy chọn) Kiểm tra tương tự các biến còn lại có dính nháy không: `JwtOptions__SecretKey`, `AuthRateLimitOptions__BffSharedSecret`, `Cors__AllowedOrigins__0` (origin có nháy sẽ làm CORS sai). Với JWT/BFF, nháy vẫn hợp lệ nhưng làm giá trị khác với FE.

## Dọn dẹp và cam kết

- Đã tạo đúng 1 user thử trên production (`smoke.531d2621@example.com`, id `bc0f17db-...`) và **đã xóa** user đó cùng `refresh_tokens` và `user_roles` của nó (xóa 1/1/1 dòng); truy vấn lại cho 0 user `smoke.%@example.com`. Không có user smoke nào khác tồn tại.
- Không sửa schema production, không sửa mã nguồn. Project Npgsql tạm trong `%TEMP%\dbcheck` đã xóa. Các process `Tripory.API` đã dừng.
- Không ghi host hay mật khẩu vào báo cáo. File thô (đã không chứa secret) nằm ở `.agents/tasks/render500/` (log API local, body, response).
