# HƯỚNG DẪN TRIỂN KHAI BƯỚC 5: PERSISTENCE LAYER (POSTGRESQL + EF CORE)
> **Mục tiêu:** Cung cấp tài liệu 5W1H và mã nguồn chuẩn cho tầng lưu trữ dữ liệu `Tripory.Persistence`.  
> **Nguyên tắc:** Fluent API 100% (không dùng Data Annotations trên Entity), tích hợp PostgreSQL + PostGIS (NetTopologySuite), quản lý Backing Fields cho Rich Domain và tự động gán Audit.

---

## 1. Cập Nhật Packages: `Tripory.Persistence.csproj`
* **What:** File cấu hình dự án của tầng Persistence.
* **Why:** Cần cài đặt EF Core, Npgsql PostgreSQL Provider và NetTopologySuite để làm việc với PostgreSQL và dữ liệu tọa độ địa lý PostGIS.
* **Where:** `src/Services/Tripory/Tripory.Persistence/Tripory.Persistence.csproj`
* **How:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Core\BuildingBlocks.Core.csproj" />
    <ProjectReference Include="..\Tripory.Domain\Tripory.Domain.csproj" />
    <ProjectReference Include="..\Tripory.Application\Tripory.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.11">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="10.0.3" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.11" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.11" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

---

## 2. `Configurations/UserConfiguration.cs`
* **What:** Cấu hình Fluent API cho Entity `User` trong EF Core.
* **Why:** 
  * Áp dụng schema `identity`, chỉ định Primary Key và tạo Index UNIQUE cho `email`, `handle`.
  * Dùng `HasConversion` để map Value Object `Email` và `Handle` hai chiều (Domain VO <-> string trong DB).
  * Cấu hình Backing Fields (`_userRoles`, `_refreshTokens`) với `PropertyAccessMode.Field` để EF Core có thể nạp dữ liệu vào private collection mà không phá vỡ tính đóng gói của Rich Domain Model.
* **Where:** `src/Services/Tripory/Tripory.Persistence/Configurations/UserConfiguration.cs`
* **Who:** Do EF Core `ApplicationDbContext` nạp trong hàm `OnModelCreating`.
* **When:** Kích hoạt khi khởi tạo DbContext hoặc chạy Migrations.
* **How:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");

        builder.HasKey(u => u.Id);

        // Value Object Email mapping
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value).Value)
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        // Value Object Handle mapping
        builder.Property(u => u.Handle)
            .HasConversion(
                handle => handle.Value,
                value => Handle.Create(value).Value)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(u => u.Handle)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.Bio)
            .HasMaxLength(User.MaxBioLength);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.BannedReason)
            .HasMaxLength(500);

        // Audit fields
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        // Backing Field cho UserRoles (1 - N)
        builder.HasMany(u => u.UserRoles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Backing Field cho RefreshTokens (1 - N)
        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.RefreshTokens)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

---

## 3. `Configurations/RoleConfiguration.cs`
* **What:** Cấu hình bảng `roles` và Seed Data cho 4 vai trò cố định.
* **Why:** 
  * Cố định mã vai trò (`Id`) theo enum `UserRoleType`, không tự tăng.
  * Tự động khởi tạo dữ liệu mẫu (`HasData`) để hệ thống có sẵn danh mục Role ngay sau khi chạy migration.
* **Where:** `src/Services/Tripory/Tripory.Persistence/Configurations/RoleConfiguration.cs`
* **How:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;
using Tripory.Domain.Enums;

namespace Tripory.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "identity");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(250);

        // Seed Data 4 vai trò chuẩn theo BRD
        builder.HasData(
            new Role(UserRoleType.Admin, "Quản trị viên toàn quyền hệ thống"),
            new Role(UserRoleType.Traveler, "Người dùng du lịch phổ thông"),
            new Role(UserRoleType.Creator, "Travel Blogger / Nhà sáng tạo nội dung"),
            new Role(UserRoleType.ServiceProvider, "Nhà cung cấp dịch vụ du lịch (Khách sạn, Tour, Nhà hàng)")
        );
    }
}
```

---

## 4. `Configurations/UserRoleConfiguration.cs`
* **What:** Cấu hình bảng nối `user_roles`.
* **Why:** Định nghĩa khóa chính kết hợp Composite Key `(UserId, RoleId)` cho quan hệ Many-to-Many giữa User và Role.
* **Where:** `src/Services/Tripory/Tripory.Persistence/Configurations/UserRoleConfiguration.cs`
* **How:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", "identity");

        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## 5. `Configurations/RefreshTokenConfiguration.cs`
* **What:** Cấu hình bảng `refresh_tokens`.
* **Why:** Lưu trữ token xác thực xoay vòng, đánh Index trên cột `token` để tối ưu tốc độ tra cứu khi người dùng gọi API Refresh Token.
* **Where:** `src/Services/Tripory/Tripory.Persistence/Configurations/RefreshTokenConfiguration.cs`
* **How:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "identity");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(rt => rt.Token);

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(50);

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(50);

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(256);

        builder.Property(rt => rt.ExpiresAt).IsRequired();
        builder.Property(rt => rt.CreatedAt).IsRequired();
    }
}
```

---

## 6. `ApplicationDbContext.cs`
* **What:** DbContext trung tâm của Service Tripory.
* **Why:** 
  * Quản lý các `DbSet` thực thể.
  * Tự động nạp toàn bộ cấu hình Fluent API trong Assembly.
  * Override `SaveChangesAsync` để tự động gán `UpdatedAt = DateTimeOffset.UtcNow` mỗi khi có thực thể bị chỉnh sửa.
* **Where:** `src/Services/Tripory/Tripory.Persistence/ApplicationDbContext.cs`
* **How:**
```csharp
using BuildingBlocks.Core.Domains.Abstractions;
using Microsoft.EntityFrameworkCore;
using Tripory.Domain.Entities;

namespace Tripory.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tự động nạp tất cả IEntityTypeConfiguration trong assembly này
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<EntityAuditBase<Guid>>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;

                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 7. `Repositories/UserRepository.cs`
* **What:** Hiện thực hóa interface `IUserRepository` được khai báo tại Application Layer.
* **Why:** 
  * Cung cấp các thao tác đọc/ghi User qua EF Core.
  * Luôn Include sẵn `UserRoles` và `RefreshTokens` khi tìm theo ID/Email/Handle để Aggregate Root `User` có đầy đủ dữ liệu khi thực thi các rule nghiệp vụ.
* **Where:** `src/Services/Tripory/Tripory.Persistence/Repositories/UserRepository.cs`
* **How:**
```csharp
using Microsoft.EntityFrameworkCore;
using Tripory.Application.Abstractions.Data;
using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User?> GetByHandleAsync(Handle handle, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Handle == handle, ct);
    }

    public async Task<bool> IsEmailUniqueAsync(Email email, CancellationToken ct = default)
    {
        return !await _context.Users.AnyAsync(u => u.Email == email, ct);
    }

    public async Task<bool> IsHandleUniqueAsync(Handle handle, CancellationToken ct = default)
    {
        return !await _context.Users.AnyAsync(u => u.Handle == handle, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }
}
```

---

## 8. `Repositories/UnitOfWork.cs`
* **What:** Hiện thực hóa `IUnitOfWork` từ `BuildingBlocks.Core`.
* **Why:** Quản lý giao dịch và commit toàn bộ thay đổi dữ liệu trong 1 request xuống cơ sở dữ liệu.
* **Where:** `src/Services/Tripory/Tripory.Persistence/Repositories/UnitOfWork.cs`
* **How:**
```csharp
using BuildingBlocks.Core.Abstractions.Persistence;

namespace Tripory.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
```

---

## 9. `DependencyInjection.cs`
* **What:** Extension method `AddPersistence()` để đăng ký các dịch vụ tầng Persistence vào DI Container.
* **Why:** 
  * Tự đóng gói cấu hình kết nối PostgreSQL + PostGIS (NetTopologySuite).
  * Đăng ký `IUserRepository` và `IUnitOfWork` với vòng đời Scoped.
* **Where:** `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs`
* **How:**
```csharp
using BuildingBlocks.Core.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Abstractions.Data;
using Tripory.Persistence.Repositories;

namespace Tripory.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=tripory_db;Username=postgres;Password=postgres";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseNetTopologySuite(); // Kích hoạt PostGIS spatial support
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

---

## 🎯 Tiêu Chuẩn Xác Nhận (Verification)
Sau khi anh gõ xong các file trên vào project `Tripory.Persistence`, anh chạy lệnh:
```bash
dotnet build src/Services/Tripory/Tripory.Persistence/Tripory.Persistence.csproj
```
**Mục tiêu:** `Build succeeded. 0 Warning(s), 0 Error(s)`.
