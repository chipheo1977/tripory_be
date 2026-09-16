# Software Engineering Core Concepts & Principles

Tài liệu đúc kết các nguyên lý thiết kế phần mềm cốt lõi (SOLID, DRY, KISS, YAGNI, GRASP, Design Patterns tổng quát...) được áp dụng trong quá trình xây dựng hệ thống `tripory_be`.

---

## 1. SOLID: Liskov Substitution Principle (LSP - Nguyên lý Thay thế Liskov)

* **Phát biểu nguyên lý:**
  > *"Các đối tượng của lớp con (derived/subtype) phải có thể thay thế hoàn toàn cho các đối tượng của lớp cha (base/supertype) mà không làm thay đổi tính đúng đắn và hành vi mong đợi của chương trình."*
  > — Barbara Liskov

* **Bản chất cốt lõi:**
  1. Lớp con không được phá vỡ các giả định/hợp đồng (contracts & invariants) mà lớp cha hoặc interface đã cam kết với bên gọi (client).
  2. Lớp con không được ném ra exception bất ngờ đối với các method mà lớp cha cho phép chạy bình thường (ví dụ kinh điển: ném `NotImplementedException`).
  3. **Quy tắc điều kiện:**
     * **Contravariance của tham số:** Lớp con không được đòi hỏi điều kiện đầu vào khắt khe hơn lớp cha (Preconditions cannot be strengthened).
     * **Covariance của kết quả:** Lớp con không được trả về kết quả lỏng lẻo hơn hoặc vi phạm cam kết đầu ra của lớp cha (Postconditions cannot be weakened).
     * **Bảo toàn Invariants:** Lớp con phải duy trì mọi bất biến trạng thái của lớp cha.

* **Ví dụ Vi phạm Kinh điển (Anti-pattern):**
  * **Square kế thừa Rectangle:** Class `Square` kế thừa `Rectangle`, khi override `SetWidth(w)` thì tự ý đổi luôn `Height = w`. Client gọi code `rect.SetWidth(5); rect.SetHeight(10); Assert(rect.Area() == 50)` sẽ bị fail khi truyền `Square`.
  * **Throwing Not Supported:** Class `ReadOnlyList` kế thừa `List` nhưng method `Add()` lại `throw new NotSupportedException()`.

* **Ứng dụng thực tế trong kiến trúc Tripory Backend:**
  * **Base Entities & Audit Entities:** [`EntityAuditBase<TKey>`]. Bất kỳ logic nào nhận `EntityBase` đều có thể nhận `EntityAuditBase` mà hành vi kiểm tra `Id`, so sánh equality vẫn giữ nguyên tính đúng đắn.
  * **CQRS Handlers:** [`ICommandHandler<TCommand>`] và [`IQueryHandler<TQuery, TResponse>`]thay thế trực tiếp `IRequestHandler<TCommand, Result>` của MediatR một cách trơn tru, bảo đảm hợp đồng trả về `Result` thống nhất.

---

## 2. Class Invariant (Bất Biến Của Lớp)

* **Định nghĩa cốt lõi:**
  > *"Class Invariant là điều kiện / quy tắc ràng buộc trạng thái mà đối tượng bắt buộc phải thỏa mãn tại mọi thời điểm tồn tại hợp lệ (sau khi khởi tạo và sau mỗi lần thực thi method public)."*
  > $\rightarrow$ **"Không được phá vỡ quy tắc ràng buộc"**

* **Bản chất trong Domain-Driven Design (DDD):**
  * **Encapsulation:** Đối tượng tự bảo vệ invariant của chính nó; không cho phép bên ngoài gán trực tiếp dữ liệu làm trạng thái trở nên không hợp lệ.
  * Mọi biến đổi trạng thái phải đi qua method có kiểm tra invariant (ví dụ: `itinerary.Publish()` kiểm tra xem có ít nhất 1 stop chưa; nếu vi phạm thì từ chối đổi trạng thái).
  * Lớp con khi kế thừa không bao giờ được phép làm suy yếu hoặc phá vỡ các invariants đã định nghĩa ở lớp cha.

---

## 3. Type Variance (Biến Thiên Kiểu Dữ Liệu Trong Hệ Thống Kiểu)

Quy định mối quan hệ kế thừa giữa các kiểu phức hợp (Generic, Delegate) dựa trên mối quan hệ giữa các kiểu con thành phần:

```text
               ┌────────────────────────────────────────────────────────┐
               │                      Type Variance                     │
               └───────┬───────────────────────┬────────────────┬───────┘
                       │                       │                │
                       ▼                       ▼                ▼
                1. Invariance           2. Covariance    3. Contravariance
                (Bất biến)              (Đồng biến)      (Nghịch biến)
                Vừa Đọc vừa Ghi         Chỉ Đọc (Out)    Chỉ Ghi / Nhận xử lý (In)
                Bắt buộc đúng kiểu      Cho phép cụ thể  Cho phép tổng quát hơn
```

### 3.1. Invariance (Bất biến kiểu)
* **Quy tắc:** **Vừa Đọc vừa Ghi $\rightarrow$ bắt buộc phải giữ đúng kiểu tuyệt đối.**
* **Giải thích:** Nếu một cấu trúc dữ liệu cho phép cả ghi dữ liệu vào lẫn đọc dữ liệu ra (ví dụ: `IList<T>`, `List<T>`), kiểu generic bắt buộc phải bất biến.
* **Tại sao:** Nếu cho phép gán `List<Dog>` vào `List<Animal>`, ta có thể ghi một con `Cat` vào danh sách thông qua biến `List<Animal>`, và khi `List<Dog>` đọc ra sẽ nhận phải `Cat` $\rightarrow$ Crash chương trình (phá vỡ type safety tại runtime).

### 3.2. Covariance (Đồng biến kiểu - `out` trong C#)
* **Quy tắc:** **Áp dụng cho đối tượng Chỉ Đọc (Producer / Output) $\rightarrow$ không cần giữ kiểu tuyệt đối.**
* **Giải thích:** Bảo toàn chiều quan hệ kế thừa: Nếu `Dog` là `Animal`, thì `IEnumerable<Dog>` cũng là `IEnumerable<Animal>`.
* **Tại sao an toàn:** Bên nhận chỉ đọc dữ liệu ra. Người cần đọc danh sách các `Animal` thì khi nhận toàn `Dog` đọc ra vẫn hoàn toàn hợp lệ (bởi vì mọi `Dog` đều là `Animal`).

### 3.3. Contravariance (Nghịch biến kiểu - `in` trong C#)
* **Quy tắc:** **Áp dụng cho đối tượng Chỉ Ghi / Tiếp nhận xử lý (Consumer / Input Parameter) $\rightarrow$ Đảo ngược chiều kế thừa.**
* **Giải thích:** Một hàm biết cách xử lý mức tổng quát (`Animal`) thì luôn thừa khả năng xử lý mức cụ thể (`Dog`).
  * Ví dụ: Nếu bạn có một hành động "Khám bệnh cho Animal" (`Action<Animal>`), bạn hoàn toàn có thể dùng nó ở bất kỳ nơi nào cần một hành động "Khám bệnh cho Dog" (`Action<Dog>`).
* **Ứng dụng thực tế trong Tripory CQRS:**
  * [`ICommandHandler<in TCommand>`]: Handler đóng vai trò là Consumer (chỉ nhận command vào để xử lý).
  * [`IDomainEventHandler<in TEvent>`]: Event Handler tiếp nhận event để xử lý. Việc dùng `in` giúp kiến trúc linh hoạt: một handler xử lý sự kiện mức cơ sở có thể tự động xử lý mọi sự kiện con phát sinh.
