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