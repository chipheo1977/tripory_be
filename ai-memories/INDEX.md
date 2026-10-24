# AI Memories – Tripory Backend Knowledge Base

Mục lục tổng hợp các tri thức, quyết định thiết kế và bài học kinh nghiệm được ghi nhận tự động bởi skill `extract-knowledge`.

---

## 1. Danh Mục Tri Thức

| Tài liệu | Mô tả |
|---|---|
| [architectural-decisions.md](./architectural-decisions.md) | Các quyết định kiến trúc cốt lõi, design patterns, nguyên tắc phân tầng và đánh đổi kỹ thuật. |
| [domain-insights.md](./domain-insights.md) | Business logic, invariants, state transitions và thuật ngữ nghiệp vụ bám sát BRD. |
| [csharp-dotnet-concepts.md](./csharp-dotnet-concepts.md) | Các khái niệm kỹ thuật chuyên sâu về C# (.NET Runtime, Generics, Memory, Async, Patterns...). |
| [software-core-concepts.md](./software-core-concepts.md) | Các nguyên lý kỹ nghệ phần mềm nền tảng (SOLID, DRY, KISS, GRASP, Design Patterns...). |
| [gotchas-and-bugs.md](./gotchas-and-bugs.md) | Các cạm bẫy kỹ thuật, lỗi đã gặp (EF Core, PostGIS, MediatR, DI...) và giải pháp xử lý. |
| [auth-rate-limiting-extra-knowledge.md](./auth-rate-limiting-extra-knowledge.md) | Extra knowledge: rate limit endpoint auth (Sliding Window, partition theo IP), lấy IP thật sau BFF qua X-Forwarded-For + shared secret, secret lộ thì sao, giới hạn của cookie HttpOnly, Options Pattern, bẫy đã gặp. |
| [auth-logout-cors-jwt-lifetime-extra-knowledge.md](./auth-logout-cors-jwt-lifetime-extra-knowledge.md) | Extra knowledge: logout thu hồi 1 refresh token (idempotent, không lộ token của ai), CORS theo cấu hình với AllowCredentials + SignalR, thời hạn token theo JwtOptions, bẫy Token.Compromised sau logout. |

---

## 2. Quy Tắc Ghi Nhận
* **Deduplication:** Luôn đối chiếu mục tương ứng trước khi cập nhật để tránh lặp lại thông tin.
* **Format:** Ghi rõ bối cảnh (Context), vấn đề/quy tắc (Problem/Rule), giải pháp (Solution/Takeaway) và file liên quan (Related Files).

