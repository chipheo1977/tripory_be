---
name: extract-knowledge
description: >-
  Extract and archive key insights, architectural decisions, domain invariants,
  and technical gotchas/bugs into tripory_be/ai-memories/. Use this skill whenever
  the user asks to save/extract knowledge, after significant architecture changes,
  or when discovering/resolving non-trivial bugs.
---

# Extract Knowledge Skill

Kỹ năng này chịu trách nhiệm trích xuất các tri thức trọng yếu, quyết định kiến trúc, business invariants và bài học kỹ thuật (bugs/gotchas) từ phiên làm việc và lưu trữ vào thư mục `ai-memories/`.

## Thư Mục Đích: `ai-memories/`

Mọi thông tin được trích xuất phải được phân loại và ghi nhận vào đúng một trong các tệp sau:
1. `ai-memories/architectural-decisions.md`: Quyết định kiến trúc, pattern (Clean Arch, DDD, CQRS), lý do chọn giải pháp, nguyên tắc phân tầng.
2. `ai-memories/domain-insights.md`: Các business rules, invariants của Entity/VO, state machine, dữ liệu nghiệp vụ đối chiếu từ tài liệu BRD (`../tripory/docs`).
3. `ai-memories/gotchas-and-bugs.md`: Các lỗi kỹ thuật (EF Core, PostGIS, MediatR pipeline, DI, build/runtime), nguyên nhân gốc rễ (Root Cause) và giải pháp xử lý.
4. `ai-memories/INDEX.md`: Cập nhật mục lục tổng hợp nếu có chủ đề lớn mới phát sinh.

5. `ai-memories/csharp-dotnet-concepts.md`: Các khái niệm chuyên sâu C# / .NET Runtime (Generic Variance, Memory/Span, Async/Await, Pattern Matching, Implicit/Explicit Operators...).
---

## Quy Trình 4 Bước Trích Xuất Tri Thức

### Bước 1: Khảo sát & Nhận diện Tri Thức (Identify)
Rà soát lại toàn bộ ngữ cảnh hội thoại hoặc phiên làm việc gần nhất để trích ra:
* **Architecture:** Có pattern nào mới được áp dụng không? Có thỏa hiệp kỹ thuật (trade-off) nào vừa được quyết định không?
* **Domain:** Có invariant nào vừa được định nghĩa (ví dụ: validation range của Value Object, trạng thái Entity chuyển đổi ra sao)?
* **Gotchas/Bugs:** Có lỗi build, cấu hình sai, hoặc ngoại lệ runtime nào vừa giải quyết thành công không?

### Bước 2: Kiểm Tra Trùng Lặp (Deduplication Check)
* Đọc tệp tương ứng trong `ai-memories/` (sử dụng `view_file` hoặc `grep_search`).
* Nếu thông tin đã tồn tại: Cập nhật hoặc bổ sung góc nhìn/bài học mới vào mục hiện có.
* Nếu thông tin mới: Thêm mục mới theo định dạng chuẩn bên dưới.

### Bước 3: Ghi Nhận Theo Định Dạng Chuẩn (Structured Entry)

Mỗi entry thêm mới cần tuân thủ cấu trúc sau:

#### Đối với Architectural Decisions (`architectural-decisions.md`):
```markdown
### [ADR-XXX] <Tên Quyết Định>
* **Ngày ghi nhận:** YYYY-MM-DD
* **Bối cảnh (Context):** Vấn đề hoặc yêu cầu kỹ thuật cần giải quyết.
* **Quyết định (Decision):** Giải pháp kiến trúc được chọn và lý do.
* **Đánh đổi (Trade-offs):** Điểm mạnh và điểm yếu đã chấp nhận.
* **Files liên quan:** Liên kết đến file code hoặc spec liên quan.
```

#### Đối với Domain Insights (`domain-insights.md`):
```markdown
### [DOMAIN] <Tên Khái Niệm / Invariant>
* **Ngày ghi nhận:** YYYY-MM-DD
* **Tài liệu nguồn:** Trích dẫn BRD / Spec tại `../tripory/docs`.
* **Quy tắc nghiệp vụ (Business Rules):** Các điều kiện ràng buộc, invariant bảo vệ bởi Entity / Value Object.
* **Files liên quan:** Liên kết đến Entity, Value Object hoặc Domain Service.
```

#### Đối với C# & .NET Concepts (`csharp-dotnet-concepts.md`):
```markdown
## <Tên Khái Niệm / Kỹ Thuật>
* **Codebase reference:** Liên kết đến file C# minh họa trong source code.
* **Khái niệm:** Bản chất kỹ thuật, cơ chế hoạt động trong CLR / Compiler.
* **Tại sao áp dụng:** Lý do đưa vào kiến trúc dự án và lợi ích mang lại.
```

#### Đối với Gotchas & Bugs (`gotchas-and-bugs.md`):
```markdown
### [BUG/GOTCHA] <Tên Lỗi hoặc Cạm Bẫy>
* **Ngày ghi nhận:** YYYY-MM-DD
* **Triệu chứng (Symptoms):** Lỗi hiển thị, stacktrace hoặc hành vi bất thường.
* **Nguyên nhân gốc rễ (Root Cause):** Tại sao xảy ra lỗi.
* **Giải pháp (Resolution):** Cách khắc phục dứt điểm.
* **Bài học kinh nghiệm:** Quy tắc phòng tránh cho tương lai.
```

### Bước 4: Báo Cáo Tóm Tắt Cho User (Report)
Sau khi ghi nhận:
* Thông báo ngắn gọn cho User về những tri thức vừa được trích xuất và file tương ứng trong `ai-memories/`.
* Cung cấp đường dẫn markdown link dạng file clickable tới file đã cập nhật.

