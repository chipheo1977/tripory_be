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