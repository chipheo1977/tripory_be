---
title: Rate limit endpoint auth — là gì, vì sao cần, khi nào cần (kèm BFF, IP thật, Options Pattern)
description: Giải thích rate limiting (Sliding Window, partition theo IP, 429), vì sao đứng sau BFF phải lấy IP thật bằng X-Forwarded-For + shared secret, và Options Pattern dùng để cấu hình ngưỡng; kèm bẫy đã gặp khi làm pha 1
projects: [tripory-be]
type: knowledge
created: 2026-10-16
tags: [extra-knowledge, rate-limiting, security, bff, aspnetcore, options-pattern]
---

# Rate Limit Cho Endpoint Auth (Extra Knowledge)

## Tóm tắt
- **Rate limit** đếm số request của một "khách" (ở đây là một IP) trong một khoảng thời gian; vượt ngưỡng thì trả `429 Too Many Requests` ngay, request không vào tới Controller.
- Backend đứng **sau BFF** (server của FE trên Vercel), nên chỉ thấy IP của BFF. Muốn đếm theo người dùng thật, BFF phải gửi `X-Forwarded-For` kèm `X-Bff-Secret`, và BE chỉ tin header đó khi secret khớp.
- Ngưỡng nằm trong class options (Options Pattern), có mặc định trong code, ghi đè qua config hoặc biến môi trường, kiểm tra hợp lệ ngay lúc khởi động.

## Rate limiting là gì
Giống nội quy "mỗi khách tối đa 5 ly mỗi giờ". Ba khái niệm cần nắm:

| Khái niệm | Nghĩa | Trong tripory_be |
|---|---|---|
| **Policy** | Một bộ luật có tên, gắn vào endpoint | `auth-login`, `auth-register`, `auth-refresh-token` |
| **Partition** | Cách chia "khách" để mỗi khách có bộ đếm riêng | Theo IP client |
| **Thuật toán cửa sổ** | Cách đếm theo thời gian | Sliding Window, 6 segment, `QueueLimit = 0` |

**Fixed Window và Sliding Window khác nhau thế nào:** Fixed Window reset bộ đếm ở mốc cố định, nên có thể gửi 5 request ở giây 59 rồi 5 request ở giây 61, tức là lọt 10 request trong 2 giây. Sliding Window chia cửa sổ 60 giây thành 6 đoạn 10 giây; mỗi 10 giây, đoạn cũ nhất trượt ra và trả lại lượt, nên không có burst ở ranh giới.

**`Retry-After` với Sliding Window:** khác Fixed Window, khi từ chối `SlidingWindowRateLimiter` **không** gắn metadata `RetryAfter` vào lease (đã kiểm chứng bằng cách chạy thật ngày 2026-10-16; chưa đối chiếu mã nguồn .NET). Lý do về bản chất: segment trượt ra có thể không trả lại lượt nào, nên limiter không biết chắc lúc nào có lượt. Dự án tự tính `Retry-After = WindowSeconds / SegmentsPerWindow` (login 10 giây, register 600 giây): đây là thời điểm **sớm nhất** có thể có lượt, thử lại đúng lúc đó vẫn có thể bị 429.

**`QueueLimit = 0`:** vượt ngưỡng thì từ chối ngay. Nếu cho xếp hàng, request của kẻ tấn công vẫn giữ kết nối và cuối cùng vẫn được xử lý.

## Tại sao cần
Không có rate limit thì:
- `login` cho thử mật khẩu không giới hạn (brute-force, credential stuffing).
- `register` cho tạo tài khoản rác không giới hạn; mỗi lần còn tốn CPU để băm mật khẩu.

Mục này được FE ghi vào danh sách phụ thuộc BE (mức "Cao") ở `tripory_fe/.planning/phases/03-identity-session-bff/03-BE-DEPENDENCIES.md`, mục #2. Tài liệu nghiệp vụ (`tripory/docs/AUTH_AND_USER_01/PT-01_authentication.md`, BR_AUTH_07) có nêu mục tiêu chống brute-force nhưng **không** nêu ngưỡng; các ngưỡng dưới đây là do Tech Lead duyệt ngày 2026-10-15.

| Endpoint | Ngưỡng | Lý do |
|---|---|---|
| `login` | 5 / 60 giây | Người thật gõ sai 1–2 lần; đủ làm chậm brute-force |
| `register` | 3 / 3600 giây | Hiếm ai đăng ký nhiều tài khoản |
| `refresh-token` | 20 / 60 giây | Tự động, nhiều tab có thể refresh cùng lúc |

## BFF và IP thật của client
**BFF (Backend For Frontend)** là server trung gian dành riêng cho một frontend. Ở Tripory, BFF giữ access/refresh token trong cookie `HttpOnly` (JavaScript không đọc được) và gọi BE thay trình duyệt.

Hệ quả cho rate limit: mọi request tới BE đều xuất phát từ IP của Vercel. Nếu partition theo `RemoteIpAddress`, mọi người dùng chung **một** bộ đếm, và một kẻ tấn công làm khóa đăng nhập của tất cả.

Cách giải quyết trong [ClientIpResolver.cs](../src/Services/Tripory/Tripory.API/RateLimiting/ClientIpResolver.cs):
1. Request có `X-Bff-Secret` khớp với config thì lấy entry đầu của `X-Forwarded-For` (IP gốc của client).
2. Không có secret, secret sai, hoặc IP không parse được thì dùng `RemoteIpAddress`.

**Tại sao cần secret:** ai cũng tự gắn được `X-Forwarded-For`. Nếu tin vô điều kiện, kẻ tấn công đổi IP giả mỗi request là né được giới hạn.

**Tại sao không dùng `UseForwardedHeaders`:** middleware đó xác định proxy tin cậy theo IP (`KnownProxies`/`KnownNetworks`), mà Vercel không có dải IP cố định.

**Tại sao so sánh bằng `CryptographicOperations.FixedTimeEquals`:** thời gian so sánh không phụ thuộc vị trí ký tự sai đầu tiên, nên không đoán dần được secret qua thời gian phản hồi (timing attack).

Chưa kiểm chứng: Vercel ghi đè `x-forwarded-for` từ client, nên giá trị BFF chuyển tiếp là đáng tin. Cần đối chiếu tài liệu Vercel trước khi deploy.

**Khi BFF gửi secret đúng nhưng thiếu `X-Forwarded-For`:** chuỗi rỗng sau `Split` thành mảng rỗng, `FirstOrDefault()` trả `null`, `IPAddress.TryParse(null)` thất bại, nên BE dùng `remoteIp` (IP của BFF). Code không lỗi, nhưng mọi người dùng lại chung một bộ đếm. Đây là lỗi **im lặng**: chỉ lộ ra khi người dùng thật bắt đầu nhận 429.

### Cookie `HttpOnly` bảo vệ được gì
- JavaScript không đọc được cookie (`document.cookie` không trả về nó), nên khi bị XSS, script độc **không lấy cắp** được token để dùng ở nơi khác. Với `localStorage`, chỉ một dòng `fetch` là token bị gửi ra ngoài và dùng được tới khi hết hạn.
- **Không** chặn được hết XSS: script độc vẫn gửi request ngay trên trình duyệt nạn nhân, vì trình duyệt tự đính kèm cookie. Thiệt hại chỉ bị khoanh lại trong lúc nạn nhân mở trang. Vì vậy cookie còn cần `Secure` (chỉ gửi qua HTTPS) và `SameSite` (chống CSRF).

### Secret bảo vệ cái gì, và không bảo vệ cái gì
Secret **không phải cổng vào BE**: BE public trên internet, không có secret vẫn gọi thẳng được. Secret chỉ trả lời một câu hỏi: *"BE có tin `X-Forwarded-For` của request này không?"* Nó bảo vệ **độ tin cậy của IP** mà rate limit dựa vào.

| Nếu secret bị lộ, kẻ tấn công có thể | Không thể |
|---|---|
| **Né hoàn toàn rate limit:** ghi IP giả khác nhau mỗi request, mỗi request rơi vào một bộ đếm mới, brute-force không giới hạn | Đăng nhập không cần mật khẩu |
| **DoS nhắm vào một người:** ghi IP của nạn nhân, gửi liền 5 request, bộ đếm của IP đó cạn, nạn nhân bị 429 khi tự đăng nhập | Đọc dữ liệu, vượt qua JWT |

Cách giảm thiệt hại:
- Secret chỉ nằm trong biến môi trường phía server, cả BE lẫn BFF; không commit, không gửi xuống trình duyệt.
- Nghi bị lộ thì **rotate** (đổi) ở cả hai phía. Đây là lợi ích của việc đặt secret trong config thay vì hard-code.
- **Khóa theo tài khoản (pha 2) là lớp phòng thủ thứ hai** (defense in depth): dù né được rate limit theo IP, sai mật khẩu quá ngưỡng thì tài khoản vẫn bị khóa.

## Options Pattern
Class [AuthRateLimitOptions.cs](../src/Services/Tripory/Tripory.API/RateLimiting/AuthRateLimitOptions.cs) được map tự động từ section `AuthRateLimitOptions` trong config.
- **`SectionName` là hằng số:** gõ sai chuỗi tên section thì compiler không báo, app lặng lẽ dùng toàn mặc định.
- **Mặc định trong code:** config không khai báo gì thì app vẫn chạy với ngưỡng hợp lý; config chỉ ghi đè phần cần đổi.
- **Fail-safe:** `BffSharedSecret` mặc định rỗng thì BE không tin `X-Forwarded-For`. Kết quả là chặn chặt quá mức (mọi người chung IP BFF), nhưng không bao giờ bị lách.
- **`init` thay vì `set`:** options là singleton dùng chung toàn app (xem §5 Service Lifetimes trong [csharp-dotnet-concepts.md](./csharp-dotnet-concepts.md)); `init` chặn việc lỡ sửa ngưỡng khi app đang chạy.
- **`.ValidateOnStart()`:** cấu hình `PermitLimit = 0` thì app dừng lúc khởi động, thay vì nổ `ArgumentException` (500) ở request login đầu tiên.
- **Biến môi trường:** `AuthRateLimitOptions__Login__PermitLimit=10`. Dùng `__` thay cho `:` để vào cấp con. Secret production đặt bằng `AuthRateLimitOptions__BffSharedSecret`, không ghi vào `appsettings.json`.

## Khi nào cần / không cần
| Tình huống | Cần? |
|---|---|
| Endpoint ẩn danh, có kiểm tra mật khẩu hoặc tạo tài nguyên (login, register, quên mật khẩu) | Cần, ngưỡng chặt |
| Endpoint tự động gọi thường xuyên (refresh token) | Cần, ngưỡng rộng |
| Endpoint đã yêu cầu đăng nhập, thao tác bình thường | Thường không cần ở giai đoạn này (YAGNI) |
| Chặn brute-force một tài khoản từ **nhiều IP** | Rate limit theo IP **không đủ**; cần khóa theo tài khoản (pha 2) |
| BE chạy nhiều instance | Bộ đếm in-memory bị chia nhỏ; cần kho dùng chung (Redis) hoặc lưu trong DB |

## Phân tầng (theo AGENTS.md)
- **Giới hạn theo IP** nằm ở `Tripory.API/RateLimiting/`: đây là việc của HTTP gateway, không phải business rule.
- **Khóa theo tài khoản** (pha 2, chưa làm tại 2026-10-16) là business rule ("sai N lần thì khóa"), nên thuộc Domain/Application. Dự định lưu `FailedLoginCount`, `LockoutEndAt` trên entity `User`. Lý do chọn DB thay vì RAM: không phụ thuộc số instance và không mất khi restart.
- Rate limiter của ASP.NET Core chạy **trước** model binding, nên không đọc được email trong body. Đây là lý do khóa theo tài khoản không làm bằng policy.

## Bẫy đã gặp và cách tránh
- **Quên rằng BE đứng sau BFF.** Partition theo `RemoteIpAddress` thì một người làm khóa tất cả. Luôn hỏi "BE thấy IP của ai?" trước khi chọn partition key.
- **`set` và `init` lẫn lộn trong cùng class options.** Build vẫn pass nên dễ bỏ qua; review phải kiểm tra riêng.
- **Sai tên solution trong lệnh build.** Repo dùng `Tripory.slnx`, không phải `tripory_be.sln`; chạy `dotnet build` ở thư mục gốc là đủ.
- **Tên extension gần trùng API có sẵn.** `AddRateLimiting` (tự viết) và `AddRateLimiter` (của .NET) dễ chọn nhầm khi dùng IntelliSense.
- **Secret nằm trong `appsettings.json` được commit.** Đã xảy ra với khóa JWT của dự án; không lặp lại với `BffSharedSecret`.
- **Tưởng Sliding Window tự trả `Retry-After`.** Code đọc `TryGetMetadata(MetadataName.RetryAfter, ...)` build vẫn pass, nhưng nhánh đó không bao giờ chạy. Chỉ lộ ra khi gọi thật và xem header 429. Bài học: hành vi phụ thuộc thư viện phải kiểm chứng bằng request thật, không suy từ API.
- **Hiểu nhầm secret là "cổng vào BE".** Khi trả lời câu hỏi "secret lộ thì sao", câu trả lời đầu tiên là "kẻ tấn công thoải mái request tới BE". Thực tế BE vốn public; hậu quả thật là IP bị giả mạo, nên rate limit vô hiệu và có thể DoS nhắm vào một người. Đánh giá rủi ro sai chỗ thì đặt biện pháp phòng thủ sai chỗ.

## Tự kiểm tra
1. Vì sao Sliding Window an toàn hơn Fixed Window cho login? → Fixed Window cho burst gấp đôi ngưỡng quanh mốc reset.
2. BFF gửi secret đúng nhưng quên `X-Forwarded-For` thì BE dùng IP nào? → `RemoteIpAddress` (IP của BFF), vì `IPAddress.TryParse` thất bại; mọi người lại chung bộ đếm.
3. Quên cấu hình `BffSharedSecret` trên production thì sao? → Mọi người dùng chung bộ đếm theo IP BFF: chặt quá mức nhưng không bị lách.
4. Vì sao khóa theo tài khoản không đặt ở API? → Đó là business rule, và rate limiter chạy trước model binding nên không biết email.
5. Spam `register` có làm IP đó bị khóa `login` không? → Không, mỗi policy có bộ đếm riêng.
6. Secret bị lộ thì kẻ tấn công làm được gì? → Giả IP: né rate limit, hoặc làm cạn bộ đếm của IP nạn nhân. Không vượt được mật khẩu hay JWT.
7. Cookie `HttpOnly` có chặn hết XSS không? → Không; chỉ chặn đọc trộm token, script độc vẫn gửi request được trên trình duyệt nạn nhân.

## Liên quan
- [architectural-decisions.md](./architectural-decisions.md), ADR-001 (Dual-Token Authentication): bối cảnh của endpoint `refresh-token`.
- [csharp-dotnet-concepts.md](./csharp-dotnet-concepts.md), §5 Service Lifetimes: vì sao options là singleton.

**Why:** backend đứng sau BFF và chưa có giới hạn nào cho endpoint auth; nếu không hiểu vì sao phải lấy IP thật và vì sao cần secret thì rất dễ làm rate limit vô dụng hoặc khóa nhầm toàn bộ người dùng.
**How to apply:** khi thêm endpoint ẩn danh mới hoặc đổi hạ tầng (thêm instance, đổi host BFF), đọc lại phần "Khi nào cần" và "BFF và IP thật" trước khi chọn policy và partition key.
