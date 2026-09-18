using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tripory.Domain.Entities;

namespace Tripory.Persistence.Configurations;

public class UserRoleConfigration : IEntityTypeConfiguration<UserRole>
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