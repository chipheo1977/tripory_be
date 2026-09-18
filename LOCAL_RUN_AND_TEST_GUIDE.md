# HƯỚNG DẪN KHỞI CHẠY LOCAL & KIỂM THỬ API HỆ THỐNG TRIPORY
### (Local Environment Setup, Database Migration & API Testing Playbook)

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 01 – Identity Foundation  
> **Cập nhật:** 18/09/2026  
> **Mục đích:** Cung cấp đầy đủ các lệnh CLI để dựng cơ sở dữ liệu PostgreSQL (PostGIS), áp migration vào database, chạy Web API và kịch bản test toàn diện qua Swagger UI / cURL.

---

## 📌 MỤC LỤC
1. [Khái Niệm: "Tạo Migration Áp Vào Postgres" Là Gì?](#1-khái-niệm-tạo-migration-áp-vào-postgres-là-gì)
2. [Tổng Hợp Các Lệnh CLI Khởi Chạy Hệ Thống](#2-tổng-hợp-các-lệnh-cli-khởi-chạy-hệ-thống)
3. [Kịch Bản Kiểm Thử 7 API Chi Tiết (Swagger & cURL)](#3-kịch-bản-kiểm-thử-7-api-chi-tiết-swagger--curl)
4. [Các Lệnh Tiện Ích Quản Lý Database & Docker](#4-các-lệnh-tiện-ích-quản-lý-database--docker)

---

## 1. KHÁI NIỆM: "TẠO MIGRATION ÁP VÀO POSTGRES" LÀ GÌ?

Trong Entity Framework Core, quá trình chuyển đổi từ mã nguồn C# sang Database gồm 2 chặng:

```
[Mã C# Entities & Configurations]
               │
               ▼ (Chặng 1: dotnet ef migrations add ...)
[File C# Migration: 20260918103036_Initial_Identity_Tables.cs]
               │
               ▼ (Chặng 2: dotnet ef database update) ◄── ĐÂY LÀ "ÁP VÀO POSTGRES"
[Database PostgreSQL Thật: Schema 'identity', các bảng 'users', 'roles'...]
```

* **File Migration (`.cs`):** Mới chỉ là **bản thiết kế kỹ thuật (Blueprint)** bằng code C#. Database PostgreSQL thực tế lúc này vẫn trống rỗng (chưa có bảng).
* **Áp Migration (`database update`):** Là hành động yêu cầu EF Core kết nối vào Postgres, dịch bản vẽ C# thành các câu lệnh SQL DDL thực thụ (`CREATE SCHEMA IF NOT EXISTS identity; CREATE TABLE identity.users...`) và thực thi chúng để tạo ra bảng vật lý sẵn sàng nhận dữ liệu.

---

## 2. TỔNG HỢP CÁC LỆNH CLI KHỞI CHẠY HỆ THỐNG

Thực hiện các lệnh sau tại thư mục gốc của dự án: `/Users/loihuu/Desktop/tripory_be`

### Bước 2.1: Bật Database PostgreSQL (PostGIS) qua Docker

```bash
# Khởi động PostgreSQL container ở chế độ chạy nền (background)
docker compose up -d

# Kiểm tra xem container đã khởi chạy thành công chưa
docker ps
```
*(Xác nhận: Container `tripory-postgres` hiển thị trạng thái `Up` và mở cổng `0.0.0.0:5432->5432/tcp`).*

---

### Bước 2.2: Cài Đặt Công Cụ `dotnet-ef` (Nếu máy chưa có)

```bash
# Cài đặt dotnet-ef toàn cục
dotnet tool install --global dotnet-ef

# (Nếu đã cài trước đó, có thể cập nhật lên bản mới nhất)
dotnet tool update --global dotnet-ef
```

---

### Bước 2.3: Áp Migration Vào Database PostgreSQL

```bash
dotnet ef database update \
  --project src/Services/Tripory/Tripory.Persistence \
  --startup-project src/Services/Tripory/Tripory.API
```
*(Xác nhận: Terminal xuất thông báo `Applying migration '20260918103036_Initial_Identity_Tables'... Done.`)*

---

### Bước 2.4: Khởi Chạy Web API Backend

**Cách 1: Chạy chuẩn**
```bash
dotnet run --project src/Services/Tripory/Tripory.API
```

**Cách 2: Chạy kèm chế độ Hot-Reload (Tự động biên dịch lại khi sửa code)**
```bash
dotnet watch --project src/Services/Tripory/Tripory.API
```

*Ứng dụng sẽ lắng nghe tại:*
* **HTTP:** `http://localhost:5251`
* **HTTPS:** `https://localhost:7071`
* **Swagger UI:** `http://localhost:5251/swagger`

---

## 3. CÁC LỆNH TIỆN ÍCH QUẢN LÝ DATABASE & DOCKER

### Xem log trực tiếp của PostgreSQL:
```bash
docker compose logs -f postgres
```

### Truy cập dòng lệnh psql bên trong container:
```bash
docker exec -it tripory-postgres psql -U postgres -d tripory_db
```
*Các câu lệnh kiểm tra nhanh trong `psql`:*
* `\dn`: Xem danh sách schemas (sẽ thấy schema `identity`).
* `\dt identity.*`: Xem danh sách các bảng trong schema `identity`.
* `SELECT * FROM identity.users;`: Xem dữ liệu người dùng vừa tạo.
* `\q`: Thoát khỏi `psql`.

### Dừng container PostgreSQL khi không sử dụng:
```bash
docker compose down
```

### Xóa sạch container và toàn bộ dữ liệu để làm lại từ đầu (Reset):
```bash
docker compose down -v
```
