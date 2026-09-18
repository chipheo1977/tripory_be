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