# HƯỚNG DẪN TỰ CODE BƯỚC 1: TẦNG DOMAIN (DOMAIN LAYER)
## Module: ITINERARY_01_MAP+TIMELINE (Hành Trình & Điểm Dừng Chân)

> **Dành cho:** Tech Lead / Developer tự tay triển khai code  
> **Vị trí lưu:** `C:\Users\OS\Documents\personal\tripory_be\GUIDE_STEP_01_DOMAIN_LAYER.md`  
> **Nguyên tắc cốt lõi:** Không sinh code sẵn; cung cấp đặc tả 5W1H, phân tích chuyên sâu **OOP & SOLID**, và các bước logic để bạn tự tay lập trình theo chuẩn **Clean Architecture + DDD (Domain-Driven Design)**.

---

## 🗺️ Cây Thư Mục Các File Cần Triển Khai Trong Bước 1

```text
src/Services/Tripory/Tripory.Domain/
├── ValueObjects/
│   ├── ItineraryTitle.cs                   # VO: Tiêu đề chuyến đi (1-100 ký tự)
│   └── Wgs84Coordinate.cs                  # VO: Tọa độ địa lý WGS84 [lng, lat]
│
├── Exceptions/
│   ├── ItineraryDomainException.cs         # Base Exception cho Domain Itinerary
│   ├── InvalidItineraryTitleException.cs   # Lỗi tiêu đề không hợp lệ
│   ├── InvalidCoordinateException.cs       # Lỗi tọa độ ngoài phạm vi địa lý
│   └── WaypointNotFoundException.cs        # Lỗi không tìm thấy điểm dừng chân
│
├── Abstractions/
│   └── External/
│       └── IGisDistanceCalculator.cs       # DIP Port: Hợp đồng tính cự ly trắc địa
│
├── Entities/
│   ├── Waypoint.cs                         # Entity: Điểm dừng chân trong ngày
│   ├── ItineraryDay.cs                     # Entity: Mốc ngày du lịch (Day 1, Day 2...)
│   └── Itinerary.cs                        # Aggregate Root: Chuyến đi du lịch
│
└── Repositories/
    └── IItineraryRepository.cs             # DIP Port: Hợp đồng lưu trữ Aggregate
```

---

## PHẦN 1: VALUE OBJECTS (ĐỐI TƯỢNG GIÁ TRỊ)

---

### FILE 1: `ItineraryTitle.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Do Aggregate `Itinerary` sở hữu; người dùng nhập khi tạo nhanh hoặc trước khi xuất bản.
* **What (Là gì):** Value Object đóng gói chuỗi tiêu đề chuyến đi, đảm bảo tính bất biến (Immutability).
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Domain/ValueObjects/ItineraryTitle.cs`.
* **When (Khi nào):** Được khởi tạo khi tạo mới lịch trình (`CreateQuickDraft`) hoặc khi cập nhật thông tin chuyến đi (`UpdateMetadata`).
* **Why (Tại sao):** Ngăn ngừa tiêu đề rỗng, chỉ chứa khoảng trắng, hoặc vượt quá 100 ký tự theo quy tắc `BR_01` trong `PT-01_metadata.md`. Thay vì kiểm tra rải rác ở khắp nơi, đóng gói invariant tại duy nhất một chỗ.
* **How (Như thế nào):** Kế thừa từ `ValueObject` (của `BuildingBlocks.Core.Domains.Abstractions.DDD`), cung cấp phương thức static factory trả về `Result<ItineraryTitle>` sau khi trim và kiểm tra độ dài.

#### 2. Biểu Diễn OOP & SOLID Trong File Này
* **Tính chất OOP thể hiện:**
  * **Tính đóng gói (Encapsulation):** Toàn bộ quy tắc về tính hợp lệ của tiêu đề (trim, kiểm tra null/rỗng, chặn vượt quá 100 ký tự) được gom kín vào bên trong class. Constructor được đặt là `private` để ngăn chặn việc `new` tùy tiện từ bên ngoài.
  * **Tính trừu tượng (Abstraction):** Biến một kiểu dữ liệu nguyên thủy vô hồn (`string`) thành một khái niệm nghiệp vụ có ngữ nghĩa rõ ràng (`ItineraryTitle`).
  * **Tính kế thừa (Inheritance):** Kế thừa từ lớp trừu tượng `ValueObject` để tự động thừa hưởng cơ chế so sánh theo giá trị (**Value Equality**) thay vì so sánh theo tham chiếu con trỏ bộ nhớ (**Reference Equality**).
* **Nguyên lý SOLID áp dụng:**
  * **S – Single Responsibility Principle (SRP):** File này chỉ có **DUY NHẤT một lý do để thay đổi**: khi quy tắc đặt tên tiêu đề chuyến đi của Tripory thay đổi (ví dụ: sau này nâng lên 150 ký tự). Nó hoàn toàn không quan tâm chuyến đi lưu ở database nào hay hiển thị ở đâu.
  * **O – Open/Closed Principle (OCP):** Khi thêm các quy tắc chuẩn hóa mới (như tự động viết hoa chữ cái đầu), code sử dụng bên ngoài không cần sửa đổi, chỉ cần mở rộng logic bên trong factory method `Create`.

#### 3. Các Bước Để Bạn Tự Code
1. **Khai báo namespace & kế thừa:**
   * Namespace: `Tripory.Domain.ValueObjects`.
   * Khai báo `public sealed class ItineraryTitle : ValueObject`.
2. **Định nghĩa hằng số & thuộc tính:**
   * Hằng số `public const int MaxLength = 100;` và `public const int MinLength = 1;`.
   * Thuộc tính `public string Value { get; }`.
3. **Thiết lập Constructor:**
   * Tạo constructor `private ItineraryTitle(string value)` để ngăn chặn việc `new` trực tiếp từ bên ngoài mà không qua kiểm tra.
4. **Viết Factory Method `Create`:**
   * Nhận vào `string? value`.
   * Bước 4.1: Kiểm tra `string.IsNullOrWhiteSpace(value)`. Nếu rỗng $\rightarrow$ trả về `Result.Failure<ItineraryTitle>` với mã lỗi `"ItineraryTitle.Empty"` và thông báo tiếng Việt: `"Tiêu đề hành trình không được để trống."`.
   * Bước 4.2: Chuẩn hóa chuỗi bằng cách `var trimmed = value.Trim();`.
   * Bước 4.3: Kiểm tra `trimmed.Length > MaxLength`. Nếu vượt quá $\rightarrow$ trả về `Result.Failure<ItineraryTitle>` với mã lỗi `"ItineraryTitle.TooLong"` và thông báo không được vượt quá 100 ký tự.
   * Bước 4.4: Trả về `Result.Success(new ItineraryTitle(trimmed))`.
5. **Ghi đè phương thức hiển thị:**
   * `public override string ToString() => Value;`
   * Có thể bổ sung toán tử ép kiểu ngầm định: `public static implicit operator string(ItineraryTitle title) => title.Value;`.

---

### FILE 2: `Wgs84Coordinate.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Thực thể `Waypoint` sử dụng để định vị địa lý điểm dừng chân trên toàn cầu.
* **What (Là gì):** Value Object lưu giữ cặp tọa độ Kinh độ (Longitude) và Vĩ độ (Latitude) theo hệ quy chiếu trắc địa chuẩn quốc tế WGS84 (EPSG:4326).
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Domain/ValueObjects/Wgs84Coordinate.cs`.
* **When (Khi nào):** Được khởi tạo khi người dùng chọn một địa điểm từ kết quả Geocoding hoặc kéo thả ghim trên bản đồ.
* **Why (Tại sao):** Bảo vệ tính toàn vẹn của không gian địa lý: Kinh độ bắt buộc nằm trong khoảng $[-180, 180]$ và Vĩ độ trong khoảng $[-90, 90]$ (Pattern 3 trong `AGENTS.md` & `PT-02_waypoint search.md`). Giúp Domain độc lập $100\%$ không bị phụ thuộc vào thư viện bên ngoài như NetTopologySuite.
* **How (Như thế nào):** Kế thừa `ValueObject`, xác thực cận trên/dưới của tọa độ, cung cấp hàm trả về tuple `(double Longitude, double Latitude)`.

#### 2. Biểu Diễn OOP & SOLID Trong File Này
* **Tính chất OOP thể hiện:**
  * **Tính đóng gói (Encapsulation):** Gắn kết chặt chẽ 2 giá trị rời rạc `Longitude` và `Latitude` thành một thực thể thống nhất. Không một ai có thể tạo ra một tọa độ "ma" có kinh độ 250 hay vĩ độ -120 vì constructor được bảo vệ và factory method tự bảo vệ invariant.
  * **Tính bất biến (Immutability):** Tọa độ địa lý của một điểm mốc là cố định; nếu muốn đổi tọa độ, phải tạo ra một instance `Wgs84Coordinate` mới thay vì sửa trực tiếp vào thuộc tính của instance cũ.
* **Nguyên lý SOLID áp dụng:**
  * **S – Single Responsibility Principle (SRP):** Chịu trách nhiệm duy nhất về tính hợp lệ của cặp tọa độ WGS84. Không ôm đồm việc tính khoảng cách (khoảng cách thuộc về GIS Calculator) hay hiển thị marker lên bản đồ.
  * **L – Liskov Substitution Principle (LSP):** Mọi đối tượng `Wgs84Coordinate` một khi đã khởi tạo thành công đều đảm bảo $100\%$ là tọa độ địa lý hợp lệ, có thể truyền vào bất kỳ thuật toán GIS nào mà không bao giờ gây ra lỗi ngoại vi tính toán.

#### 3. Các Bước Để Bạn Tự Code
1. **Khai báo namespace & kế thừa:**
   * Namespace: `Tripory.Domain.ValueObjects`.
   * Khai báo `public sealed class Wgs84Coordinate : ValueObject`.
2. **Khai báo hằng số giới hạn tọa độ:**
   * `public const double MinLongitude = -180.0;`
   * `public const double MaxLongitude = 180.0;`
   * `public const double MinLatitude = -90.0;`
   * `public const double MaxLatitude = 90.0;`
3. **Khai báo thuộc tính:**
   * `public double Longitude { get; }` (Kinh độ).
   * `public double Latitude { get; }` (Vĩ độ).
4. **Constructor riêng tư (Private Constructor):**
   * Gán giá trị cho `Longitude` và `Latitude`.
5. **Factory Method `Create`:**
   * Nhận vào `double longitude, double latitude`.
   * Kiểm tra điều kiện kinh độ: nếu `longitude < MinLongitude || longitude > MaxLongitude` $\rightarrow$ trả về `Result.Failure<Wgs84Coordinate>` với mã `"Coordinate.InvalidLongitude"`.
   * Kiểm tra điều kiện vĩ độ: nếu `latitude < MinLatitude || latitude > MaxLatitude` $\rightarrow$ trả về `Result.Failure<Wgs84Coordinate>` với mã `"Coordinate.InvalidLatitude"`.
   * Nếu hợp lệ $\rightarrow$ trả về `Result.Success(new Wgs84Coordinate(longitude, latitude))`.
6. **Phương thức tiện ích:**
   * `public (double Lng, double Lat) ToTuple() => (Longitude, Latitude);`
   * `public override string ToString() => $"[{Longitude:F6}, {Latitude:F6}]";`

---

## PHẦN 2: DOMAIN EXCEPTIONS (NGOẠI LỆ NGHIỆP VỤ)

---

### FILE 3: `ItineraryDomainException.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Base class ngoại lệ dùng chung cho toàn bộ module Lịch trình.
* **What (Là gì):** Lớp ngoại lệ kế thừa từ `DomainException` của `BuildingBlocks.Core.Domains.Abstractions.Exceptions`.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Domain/Exceptions/ItineraryDomainException.cs`.
* **When (Khi nào):** Ném ra khi có vi phạm bất biến nghiệp vụ nghiêm trọng trong quá trình xử lý logic domain.
* **Why (Tại sao):** Phân loại rạch ròi giữa lỗi nghiệp vụ (Domain Exception - HTTP 400/422) và lỗi kỹ thuật hạ tầng (System Exception - HTTP 500), giúp Global Exception Handler xử lý chính xác.
* **How (Như thế nào):** Kế thừa `DomainException` và truyền thông điệp lỗi xuống lớp cha.

#### 2. Biểu Diễn OOP & SOLID Trong File Này
* **Tính chất OOP thể hiện:**
  * **Tính kế thừa (Inheritance):** Thiết lập quan hệ *"is-a"* (`ItineraryDomainException` là một `DomainException`, và là một `System.Exception`).
  * **Tính đa hình (Polymorphism):** Ở tầng API, `GlobalExceptionHandler` chỉ cần bắt kiểu cha `DomainException`, nhưng khi runtime chạy, nó sẽ tự động nhận diện và xử lý thông điệp cụ thể từ các ngoại lệ con của module Itinerary.
* **Nguyên lý SOLID áp dụng:**
  * **L – Liskov Substitution Principle (LSP):** Mọi exception con của module Itinerary đều có thể thay thế hoàn hảo cho `DomainException` tại middleware xử lý lỗi tập trung mà không làm hỏng luồng bắt lỗi của ứng dụng.

#### 3. Các Bước Để Bạn Tự Code
1. Kế thừa `DomainException`: `public class ItineraryDomainException : DomainException`.
2. Tạo constructor nhận vào `string message`: gọi `base(message)`.

---

### FILE 4: `InvalidItineraryTitleException.cs`
* **Where:** `src/Services/Tripory/Tripory.Domain/Exceptions/InvalidItineraryTitleException.cs`.
* **Mục đích:** Kế thừa `ItineraryDomainException`, ném ra khi tiêu đề chuyến đi không đáp ứng quy chuẩn độ dài hoặc bị rỗng.
* **Biểu diễn OOP & SOLID:** Kế thừa tầng bậc (Inheritance) và tuân thủ **Single Responsibility Principle (SRP)**: mỗi exception đại diện cho đúng một nguyên nhân vi phạm nghiệp vụ cụ thể.
* **Các bước:** Tạo constructor nhận `string title` hoặc `string reason`, gọi `base(...)` kèm câu thông báo chi tiết.

---

### FILE 5: `InvalidCoordinateException.cs`
* **Where:** `src/Services/Tripory/Tripory.Domain/Exceptions/InvalidCoordinateException.cs`.
* **Mục đích:** Kế thừa `ItineraryDomainException`, ném ra khi nhận được cặp tọa độ vượt ra ngoài phạm vi địa lý toàn cầu.
* **Biểu diễn OOP & SOLID:** Thể hiện **SRP** và tính đóng gói ngữ cảnh lỗi: nhận trực tiếp giá trị sai `longitude, latitude` để format thành thông báo lỗi chính xác phục vụ debug.
* **Các bước:** Nhận vào `double longitude, double latitude`, gọi `base($"Tọa độ [{longitude}, {latitude}] không nằm trong hệ quy chiếu WGS84 hợp lệ.")`.

---

### FILE 6: `WaypointNotFoundException.cs`
* **Where:** `src/Services/Tripory/Tripory.Domain/Exceptions/WaypointNotFoundException.cs`.
* **Mục đích:** Kế thừa `ItineraryDomainException`, ném ra khi thao tác chỉnh sửa, xóa hoặc đổi thứ tự một điểm dừng chân không tồn tại trong lịch trình.
* **Biểu diễn OOP & SOLID:** Thể hiện **SRP** và **LSP**: định danh chính xác lỗi 404 cho thực thể con của Aggregate.
* **Các bước:** Nhận vào `Guid waypointId`, gọi `base($"Không tìm thấy điểm dừng chân có định danh '{waypointId}'.")`.

---

## PHẦN 3: ABSTRACTIONS & EXTERNAL PORTS (HỢP ĐỒNG GIAO TIẾP)

---

### FILE 7: `IGisDistanceCalculator.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Tầng Domain định nghĩa ra; tầng Infrastructure sẽ triển khai chi tiết; Aggregate `Itinerary` gọi để tính khoảng cách.
* **What (Là gì):** Giao diện (Port) trừu tượng hóa thuật toán tính khoảng cách địa lý (Haversine / Geodesic) giữa các tọa độ WGS84.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Domain/Abstractions/External/IGisDistanceCalculator.cs`.
* **When (Khi nào):** Được gọi mỗi khi thêm điểm mới, xóa điểm, hoặc sau khi thả thẻ sắp xếp lại thứ tự điểm dừng trong ngày.
* **Why (Tại sao):** Tuân thủ triệt để nguyên tắc **Dependency Inversion Principle (DIP - Pattern 4 trong AGENTS.md)**. Domain cần tính toán cự ly nhưng KHÔNG ĐƯỢC PHÉP phụ thuộc trực tiếp vào NetTopologySuite hay bất kỳ thư viện GIS bên thứ ba nào.
* **How (Như thế nào):** Khai báo các phương thức thuần túy nhận vào `Wgs84Coordinate` hoặc danh sách tọa độ, trả về khoảng cách dạng số thực (`double`, đơn vị km).

#### 2. Biểu Diễn OOP & SOLID Trong File Này
* **Tính chất OOP thể hiện:**
  * **Tính trừu tượng (Abstraction):** Đây là đỉnh cao của tính trừu tượng. Tầng Domain chỉ quan tâm đến câu hỏi: *"Cho tôi biết từ A đến B bao nhiêu km?"*, hoàn toàn che giấu công thức toán học phức tạp (bán kính Trái Đất, hàm Sin/Cos, lượng giác cầu).
  * **Tính đa hình (Polymorphism):** Cho phép runtime tráo đổi linh hoạt giữa nhiều lớp hiện thực khác nhau (ví dụ: Haversine Calculator đơn giản trong Unit Test, và NetTopologySuite Geodesic Calculator trong Production).
* **Nguyên lý SOLID áp dụng:**
  * **D – Dependency Inversion Principle (DIP):** Module cấp cao (`Domain`) không phụ thuộc module cấp thấp (`Infrastructure`). Interface này thuộc sở hữu của Domain. Tầng Infrastructure buộc phải tham chiếu vào Domain để triển khai.
  * **I – Interface Segregation Principle (ISP):** Interface được "may đo" cực kỳ tinh gọn, chỉ chứa đúng 2 method phục vụ tính chặng và tính lộ trình, không bị nhồi nhét các tính năng GIS dư thừa như vẽ bản đồ hay geocoding.
  * **O – Open/Closed Principle (OCP):** Khi muốn đổi từ tính đường chim bay sang tính đường bộ qua OSRM, code Domain vẫn giữ nguyên $100\%$ không đổi.

#### 3. Các Bước Để Bạn Tự Code
1. **Khai báo interface:**
   * Namespace: `Tripory.Domain.Abstractions.External`.
   * Khai báo `public interface IGisDistanceCalculator`.
2. **Khai báo phương thức tính chặng đơn:**
   * `double CalculateDistanceKm(Wgs84Coordinate from, Wgs84Coordinate to);`  
     *Ý nghĩa:* Tính khoảng cách đường chim bay giữa 2 điểm (km).
3. **Khai báo phương thức tính tổng chặng theo chuỗi điểm:**
   * `double CalculateRouteDistanceKm(IReadOnlyList<Wgs84Coordinate> coordinates);`  
     *Ý nghĩa:* Duyệt tuần tự từ điểm $0 \rightarrow 1 \rightarrow 2... \rightarrow N-1$, cộng dồn khoảng cách từng cặp điểm liền kề. Nếu danh sách $< 2$ điểm $\rightarrow$ trả về `0.0`.

---

### FILE 8: `IItineraryRepository.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Aggregate Root `Itinerary` sử dụng qua tầng Application Handlers; tầng Persistence (EF Core) sẽ implement.
* **What (Là gì):** Hợp đồng truy xuất và lưu trữ dữ liệu chuyên biệt cho Aggregate `Itinerary`.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Domain/Repositories/IItineraryRepository.cs`.
* **When (Khi nào):** Khi cần đọc/ghi dữ liệu lịch trình từ cơ sở dữ liệu.
* **Why (Tại sao):** Tách biệt logic nghiệp vụ khỏi công nghệ cơ sở dữ liệu, hỗ trợ viết Unit Test dễ dàng bằng Mock/Stub.
* **How (Như thế nào):** Kế thừa từ `IRepositoryBase<Itinerary, Guid>` (của `BuildingBlocks.Core.Domains.Abstractions`), bổ sung các phương thức truy vấn chuyên biệt kèm nạp các thực thể con (Eager Loading Days & Waypoints).

#### 2. Biểu Diễn OOP & SOLID Trong File Này
* **Tính chất OOP thể hiện:**
  * **Tính trừu tượng (Abstraction):** Che giấu hoàn toàn các chi tiết kỹ thuật của cơ sở dữ liệu (PostgreSQL, SQL query, Change Tracker của EF Core). Đối với Domain/Application, cơ sở dữ liệu chỉ giống như một tập hợp đối tượng trong bộ nhớ.
* **Nguyên lý SOLID áp dụng:**
  * **D – Dependency Inversion Principle (DIP):** Các UseCases ở tầng Application chỉ phụ thuộc vào `IItineraryRepository`, hoàn toàn không biết đến sự tồn tại của `ApplicationDbContext`.
  * **I – Interface Segregation Principle (ISP):** Kế thừa từ `IRepositoryBase` và chỉ bổ sung những phương thức thực sự cần cho Aggregate `Itinerary` (`GetByIdWithDetailsAsync`, `IsOwnerAsync`). Không ép các repository khác phải có những method này.

#### 3. Các Bước Để Bạn Tự Code
1. **Khai báo interface:**
   * Namespace: `Tripory.Domain.Repositories`.
   * `public interface IItineraryRepository : IRepositoryBase<Itinerary, Guid>`.
2. **Khai báo phương thức lấy chi tiết:**
   * `Task<Itinerary?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);`  
     *Ý nghĩa:* Lấy lịch trình kèm toàn bộ danh sách `Days` và `Waypoints`.
3. **Khai báo phương thức kiểm tra quyền sở hữu:**
   * `Task<bool> IsOwnerAsync(Guid itineraryId, Guid userId, CancellationToken ct = default);`
4. **Khai báo phương thức lấy danh sách của tôi:**
   * `Task<(IReadOnlyList<Itinerary> Items, int TotalCount)> GetMyItinerariesPagedAsync(Guid userId, int pageIndex, int pageSize, bool? isPublic, CancellationToken ct = default);`

---

## PHẦN 4: DOMAIN ENTITIES & AGGREGATE ROOT

---

### FILE 9: `Waypoint.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Do Aggregate Root `Itinerary` sở hữu và quản lý vòng đời trực tiếp; không tồn tại độc lập ngoài `Itinerary`.
* **What (Là gì):** Thực thể đại diện cho một điểm dừng chân (Point of Interest - POI) trong một ngày cụ thể của chuyến đi.
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Domain/Entities/Waypoint.cs`.
* **When (Khi nào):** Được tạo ra khi người dùng tìm kiếm địa điểm và bấm "Thêm vào Ngày X".
* **Why (Tại sao):** Lưu vết tên, địa chỉ, tọa độ không gian WGS84, thứ tự ghé thăm trong ngày (`order_index`) và ghi chú cá nhân của người du lịch.
* **How (Như thế nào):** Kế thừa `EntityAuditBase<Guid>`, đóng gói các trường thuộc tính với `private set`, chỉ cho phép `Itinerary` cập nhật thông qua internal method.

#### 2. Biểu Diễn OOP & SOLID Trong File Này
* **Tính chất OOP thể hiện:**
  * **Tính đóng gói mạnh (Strict Encapsulation):**
    * Tất cả properties đều có `private set`.
    * Constructor và các method thay đổi trạng thái (`UpdateInfo`, `UpdateOrderIndex`) đều được đánh dấu phạm vi truy cập là **`internal`**. Điều này có nghĩa: các tầng bên ngoài (API, Application Handlers) **tuyệt đối không thể tự ý sửa đổi Waypoint**; chỉ duy nhất các class cùng nằm trong assembly `Tripory.Domain` (chính là `Itinerary`) mới có quyền thao tác.
  * **Tính kế thừa (Inheritance):** Kế thừa `EntityAuditBase<Guid>` để có sẵn `Id`, `CreatedAt`, `UpdatedAt`.
* **Nguyên lý SOLID áp dụng:**
  * **S – Single Responsibility Principle (SRP):** `Waypoint` chỉ chịu trách nhiệm duy nhất là chứa và bảo vệ trạng thái của *một điểm dừng đơn lẻ*. Nó không quan tâm điểm nào đứng trước nó, điểm nào đứng sau nó, hay tổng cự ly ngày là bao nhiêu (trách nhiệm đó thuộc về Aggregate Root).

#### 3. Các Bước Để Bạn Tự Code
1. **Khai báo class:**
   * Namespace: `Tripory.Domain.Entities`.
   * `public class Waypoint : EntityAuditBase<Guid>`.
2. **Định nghĩa các trường dữ liệu (Properties với `private set`):**
   * `public Guid ItineraryId { get; private set; }`
   * `public int DayNumber { get; private set; }` (Số ngày: 1, 2, 3... - Bắt buộc $\ge 1$).
   * `public int OrderIndex { get; private set; }` (Thứ tự trong ngày: 0, 1, 2... - Bắt buộc $\ge 0$).
   * `public string Name { get; private set; } = string.Empty;` (Tên địa điểm, tối đa 200 ký tự).
   * `public string? Address { get; private set; }` (Địa chỉ chi tiết).
   * `public Wgs84Coordinate Coordinate { get; private set; } = null!;` (Tọa độ địa lý WGS84).
   * `public string? Notes { get; private set; }` (Ghi chú cá nhân).
3. **Thiết lập Constructor:**
   * `private Waypoint() { }` (dành cho EF Core).
   * Constructor `internal Waypoint(Guid id, Guid itineraryId, int dayNumber, int orderIndex, string name, string? address, Wgs84Coordinate coordinate, string? notes)`: gán các trường và cập nhật `CreatedAt`, `UpdatedAt`.
4. **Các phương thức nghiệp vụ nội bộ (Internal Methods):**
   * `internal void UpdateInfo(string name, string? address, Wgs84Coordinate coordinate, string? notes)`: Cập nhật thông tin và gán `UpdatedAt = DateTimeOffset.UtcNow`.
   * `internal void UpdateOrderIndex(int newOrderIndex)`: Cập nhật lại chỉ mục thứ tự khi có thao tác kéo thả hoặc xóa điểm khác.
   * `internal void MoveToDay(int newDayNumber, int newOrderIndex)`: Chuyển điểm sang ngày khác.

---

### FILE 10: `ItineraryDay.cs`

#### 1. Mô hình 5W1H
* **Who (Ai):** Do Aggregate Root `Itinerary` trực tiếp quản lý.
* **What (Là gì):** Thực thể đại diện cho một mốc ngày du lịch trong chuyến đi (ví dụ: Ngày 1 - Khám phá Phố Cổ, Ngày 2 - Chinh phục Mã Pí Lèng).
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Domain/Entities/ItineraryDay.cs`.
* **When (Khi nào):** Tự động sinh ra khi người dùng thêm điểm dừng ở một ngày mới, hoặc khi tạo chuyến đi mẫu.
* **Why (Tại sao):** Quản lý tiêu đề phụ của ngày (`subtitle`), lưu trữ cự ly tích lũy trong ngày (`day_distance_km`) theo quy tắc `BR_01` & `BR_04` trong `PT-05_draw route & calculation.md`.
* **How (Như thế nào):** Kế thừa `EntityBase<Guid>`, liên kết với `ItineraryId`, chứa `DayNumber`, `Subtitle`, `DayDistanceKm`.

#### 2. Biểu Diễn OOP & SOLID Trong File Này
* **Tính chất OOP thể hiện:**
  * **Tính đóng gói (Encapsulation):** Giấu kín các setter của `Subtitle` và `DayDistanceKm`. Chỉ có method `internal SetDistance` và `SetSubtitle` cho phép Aggregate Root cập nhật.
  * **Tính kế thừa (Inheritance):** Kế thừa `EntityBase<Guid>`.
* **Nguyên lý SOLID áp dụng:**
  * **S – Single Responsibility Principle (SRP):** Chịu trách nhiệm duy nhất về metadata của mốc ngày du lịch và cự ly của ngày đó. Không tự ý duyệt danh sách điểm dừng để tính khoảng cách.

#### 3. Các Bước Để Bạn Tự Code
1. **Khai báo class:**
   * Namespace: `Tripory.Domain.Entities`.
   * `public class ItineraryDay : EntityBase<Guid>`.
2. **Khai báo thuộc tính (Properties với `private set`):**
   * `public Guid ItineraryId { get; private set; }`
   * `public int DayNumber { get; private set; }` (Bắt buộc $\ge 1$).
   * `public string? Subtitle { get; private set; }` (Phụ đề của ngày, tối đa 100 ký tự).
   * `public double DayDistanceKm { get; private set; }` (Tổng khoảng cách các chặng trong ngày).
3. **Constructor:**
   * `private ItineraryDay() { }` (cho EF Core).
   * `internal ItineraryDay(Guid id, Guid itineraryId, int dayNumber, string? subtitle = null)`.
4. **Các phương thức cập nhật nội bộ:**
   * `internal void SetSubtitle(string? subtitle)`: Cắt khoảng trắng, kiểm tra $\le 100$ ký tự.
   * `internal void SetDistance(double distanceKm)`: Cập nhật `DayDistanceKm = Math.Max(0, distanceKm)`.

---

### FILE 11: `Itinerary.cs` (AGGREGATE ROOT TRUNG TÂM)

#### 1. Mô hình 5W1H
* **Who (Ai):** Thực thể trung tâm của toàn bộ hệ thống Tripory; gắn liền với `UserId` của người tạo (Traveler).
* **What (Là gì):** Aggregate Root duy nhất chịu trách nhiệm bảo vệ toàn bộ tính toàn vẹn của một chuyến đi, bao gồm metadata, các mốc ngày (`Days`), và chuỗi các điểm dừng chân (`Waypoints`).
* **Where (Ở đâu):** `src/Services/Tripory/Tripory.Domain/Entities/Itinerary.cs`.
* **When (Khi nào):** Được khởi tạo khi bắt đầu lập kế hoạch chuyến đi mới. Mọi thao tác thêm, xóa, sửa điểm dừng hoặc xuất bản đều phải thông qua Aggregate Root này.
* **Why (Tại sao):** Tuân thủ tuyệt đối quy tắc DDD trong `AGENTS.md`. Ngăn chặn tình trạng dữ liệu mồ côi, sai lệch thứ tự `order_index`, tính sai cự ly hoặc vi phạm quy tắc xuất bản (`BR_01` đến `BR_05` trong `create map+timeline.md`).
* **How (Như thế nào):** Kế thừa `EntityFullAuditBase<Guid>, IAggregateRoot`. Toàn bộ danh sách con `_waypoints` và `_days` được giấu kín dưới dạng private field và chỉ phơi ra `IReadOnlyCollection`. Mọi thay đổi trạng thái phải đi qua method nghiệp vụ.

#### 2. Biểu Diễn OOP & SOLID Trong File Này (Trọng Tâm Kiến Trúc)
* **Tính chất OOP thể hiện:**
  * **Tính đóng gói cấp độ Cụm (Cluster Encapsulation):**  
    Đây là đỉnh cao của OOP. Toàn bộ đồ thị đối tượng (`Itinerary` $\rightarrow$ `ItineraryDay` $\rightarrow$ `Waypoint`) được bao bọc trong một "lớp vỏ thép". Không ai có thể gọi `_waypoints.Add()` từ bên ngoài vì collection phơi ra là `IReadOnlyCollection`. Mọi thay đổi đều phải thông qua các method có kiểm tra luật lệ (`AddWaypoint`, `RemoveWaypoint`, `Publish`).
  * **Tính kế thừa (Inheritance):** Kế thừa `EntityFullAuditBase<Guid>` (quản lý Soft Delete và thời gian vết) và `IAggregateRoot` (đánh dấu ranh giới cụm).
* **Nguyên lý SOLID áp dụng trọn vẹn:**
  * **S – Single Responsibility Principle (SRP):** Chịu trách nhiệm duy nhất là **Người gác cổng bảo vệ tính nhất quán của chuyến đi**. Nó không tự viết thuật toán tính sin/cos trắc địa mà ủy quyền cho `IGisDistanceCalculator`.
  * **O – Open/Closed Principle (OCP):** Hàm `RecalculateDistances(IGisDistanceCalculator calculator)` sẵn sàng đón nhận bất kỳ thuật toán tính cự ly mới nào mà không cần sửa code của `Itinerary`.
  * **L – Liskov Substitution Principle (LSP):** Kế thừa hoàn hảo từ `EntityFullAuditBase`, hoạt động trơn tru với các interceptor tự động kiểm tra `IsDeleted` của EF Core.
  * **I – Interface Segregation Principle (ISP):** Phơi ra `IReadOnlyCollection<Waypoint>` thay vì `IList<Waypoint>`, giúp client không bị ép thấy các method `Add`, `Remove`, `Clear` mà họ không có quyền dùng.
  * **D – Dependency Inversion Principle (DIP):** Phương thức tính cự ly phụ thuộc vào interface trừu tượng `IGisDistanceCalculator` chứ không phụ thuộc trực tiếp vào bất kỳ thư viện ngoài nào.

#### 3. Các Bước Để Bạn Tự Code

##### Bước 3.1: Khai báo lớp & Kế thừa
* Namespace: `Tripory.Domain.Entities`.
* `public class Itinerary : EntityFullAuditBase<Guid>, IAggregateRoot`.
* Khai báo hằng số: `public const int MaxDescriptionLength = 1000;`.

##### Bước 3.2: Định nghĩa các thuộc tính Metadata
* `public Guid UserId { get; private set; }` (Khóa ngoại người sở hữu).
* `public ItineraryTitle Title { get; private set; } = null!;` (Value Object tiêu đề).
* `public string? Description { get; private set; }` (Mô tả chuyến đi).
* `public string? CoverImageUrl { get; private set; }` (Ảnh bìa).
* `public DateOnly? StartDate { get; private set; }` (Ngày khởi hành dự kiến).
* `public bool IsPublic { get; private set; }` (Trạng thái: `false` = Bản nháp, `true` = Đã xuất bản).
* `public double TotalDistanceKm { get; private set; }` (Tổng cự ly toàn chuyến).

##### Bước 3.3: Đóng gói tập hợp con (Collections)
* Khai báo `private readonly List<ItineraryDay> _days = new();`
* Phơi ra ngoài: `public IReadOnlyCollection<ItineraryDay> Days => _days.AsReadOnly();`
* Khai báo `private readonly List<Waypoint> _waypoints = new();`
* Phơi ra ngoài: `public IReadOnlyCollection<Waypoint> Waypoints => _waypoints.AsReadOnly();`

##### Bước 3.4: Factory Method `CreateQuickDraft` (Khởi tạo nhanh)
* Phương thức static: `public static Result<Itinerary> CreateQuickDraft(Guid userId, ItineraryTitle title)`
* Kiểm tra: `userId == Guid.Empty` $\rightarrow$ trả về Failure.
* Khởi tạo đối tượng `Itinerary` mới:
  * `Id = Guid.NewGuid()`
  * `UserId = userId`
  * `Title = title`
  * `IsPublic = false` (Mặc định là Bản nháp)
  * `TotalDistanceKm = 0.0`
  * `CreatedAt = DateTimeOffset.UtcNow`, `UpdatedAt = DateTimeOffset.UtcNow`
* Tự động thêm sẵn Ngày 1: Gọi hàm nội bộ tạo `ItineraryDay` với `DayNumber = 1`.
* Trả về `Result.Success(itinerary)`.

##### Bước 3.5: Phương thức `UpdateMetadata` (Cập nhật thông tin)
* Nhận vào: `ItineraryTitle title, string? description, DateOnly? startDate, string? coverImageUrl`.
* Validate mô tả: nếu `description != null && description.Length > MaxDescriptionLength` $\rightarrow$ trả về `Result.Failure` với lỗi `"Itinerary.DescriptionTooLong"`.
* Cập nhật các trường tương ứng và gán `UpdatedAt = DateTimeOffset.UtcNow`.
* Trả về `Result.Success()`.

##### Bước 3.6: Phương thức `Publish` & `Unpublish` (Vòng đời xuất bản)
* `public Result Publish()`:
  * Kiểm tra invariant: Chuyến đi bắt buộc phải có ít nhất 1 điểm dừng chân (`_waypoints.Count == 0`) $\rightarrow$ trả về Failure `"Itinerary.CannotPublishEmpty"` với thông báo: *"Không thể xuất bản hành trình khi chưa có điểm dừng chân nào."*.
  * Cập nhật `IsPublic = true; UpdatedAt = DateTimeOffset.UtcNow;`.
  * Trả về `Result.Success()`.
* `public void Unpublish()`:
  * Đặt `IsPublic = false; UpdatedAt = DateTimeOffset.UtcNow;`.

##### Bước 3.7: Phương thức `AddWaypoint` (Thêm điểm dừng chân)
* Nhận vào: `int dayNumber, string name, string? address, Wgs84Coordinate coordinate, string? notes`.
* Validate:
  * `dayNumber < 1` $\rightarrow$ báo lỗi ngày không hợp lệ.
  * `string.IsNullOrWhiteSpace(name)` $\rightarrow$ báo lỗi tên địa điểm không được để trống.
* Đảm bảo Ngày tồn tại: Kiểm tra trong `_days` xem đã có ngày số `dayNumber` chưa. Nếu chưa có $\rightarrow$ tự động tạo và thêm một `ItineraryDay` mới cho ngày đó.
* Tính toán `order_index`: Lấy các waypoint hiện có của ngày đó $\rightarrow$ `var nextOrder = _waypoints.Where(w => w.DayNumber == dayNumber).Count();`.
* Khởi tạo thực thể `Waypoint` mới với `orderIndex = nextOrder` và thêm vào danh sách `_waypoints`.
* Cập nhật `UpdatedAt = DateTimeOffset.UtcNow;`.
* Trả về `Result.Success(newWaypoint)`.

##### Bước 3.8: Phương thức `RemoveWaypoint` (Xóa & Đánh số lại thứ tự tự động)
* Nhận vào: `Guid waypointId`.
* Tìm waypoint trong `_waypoints`. Nếu không thấy $\rightarrow$ ném `WaypointNotFoundException` hoặc trả về Failure.
* Lưu lại `dayNumber` của điểm bị xóa.
* Xóa điểm khỏi `_waypoints.Remove(waypoint)`.
* **Bảo vệ Invariant chuẩn hóa thứ tự liên tục (`BR_04` trong `PT-03`):**
  * Lấy toàn bộ các điểm còn lại thuộc `dayNumber`, sắp xếp tăng dần theo `OrderIndex`.
  * Dùng vòng lặp gán lại `order_index` liên tục: `0, 1, 2, ..., N-1` bằng cách gọi `wp.UpdateOrderIndex(i)`.
* Cập nhật `UpdatedAt = DateTimeOffset.UtcNow;`.
* Trả về `Result.Success()`.

##### Bước 3.9: Phương thức `ReorderWaypointsInDay` (Kéo thả sắp xếp thứ tự)
* Nhận vào: `int dayNumber, IReadOnlyList<Guid> orderedWaypointIds`.
* Lấy danh sách các điểm hiện có của ngày `dayNumber`: `var dayWaypoints = _waypoints.Where(w => w.DayNumber == dayNumber).ToList();`.
* **Kiểm tra tính toàn vẹn (Integrity Check):**
  * Số lượng ID truyền vào phải bằng đúng số lượng điểm hiện có trong ngày (`orderedWaypointIds.Count == dayWaypoints.Count`).
  * Tất cả các ID trong `orderedWaypointIds` phải tồn tại trong `dayWaypoints`.
  * Nếu không khớp $\rightarrow$ trả về Failure `"Itinerary.InvalidReorderList"` (*"Danh sách điểm sắp xếp không khớp với dữ liệu hiện tại."*).
* Cập nhật thứ tự mới:
  * Duyệt theo danh sách `orderedWaypointIds` từ chỉ mục `0` đến `N-1`.
  * Tìm waypoint tương ứng và gọi `wp.UpdateOrderIndex(index)`.
* Cập nhật `UpdatedAt = DateTimeOffset.UtcNow;`.
* Trả về `Result.Success()`.

##### Bước 3.10: Phương thức `RecalculateDistances` (Tính cự ly chuẩn trắc địa)
* Nhận vào: `IGisDistanceCalculator calculator`.
* Khởi tạo biến tích lũy: `double totalItineraryKm = 0.0;`.
* Lặp qua từng ngày trong `_days`:
  * Lấy danh sách điểm của ngày đó, sắp xếp theo `OrderIndex` tăng dần.
  * Lấy danh sách tọa độ: `var coords = dayWaypoints.Select(w => w.Coordinate).ToList();`.
  * Gọi calculator: `var dayKm = calculator.CalculateRouteDistanceKm(coords);`.
  * Cập nhật cho ngày: `day.SetDistance(dayKm);`.
  * **Quy tắc ngắt quãng qua đêm (`BR_01` trong `PT-05`):** Cộng dồn khoảng cách ngày vào tổng chuyến đi: `totalItineraryKm += dayKm;` (Tuyệt đối không tính khoảng cách nối giữa điểm cuối ngày $N$ và điểm đầu ngày $N+1$).
* Cập nhật tổng cự ly toàn chuyến: `TotalDistanceKm = totalItineraryKm;`.

---

## 🎯 Tiêu Chuẩn Nghiệm Thu & Kiểm Chứng (Quality Gate)

Sau khi bạn hoàn thành việc viết 11 file trên:
1. Chạy lệnh kiểm tra biên dịch trong terminal:
   ```powershell
   dotnet build src/Services/Tripory/Tripory.Domain/Tripory.Domain.csproj
   ```
2. **Kỳ vọng:** `Build succeeded` với `0 Warning(s)`, `0 Error(s)`.
3. Kiểm tra tính độc lập: Tầng `Tripory.Domain` **tuyệt đối không** chứa từ khóa `using Microsoft.EntityFrameworkCore;`, `using Microsoft.AspNetCore;`, hay các Data Annotation `[Table]`, `[Column]`.
