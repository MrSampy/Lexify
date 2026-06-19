using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnName("id");

        builder.Property(rt => rt.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(rt => rt.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(rt => rt.RevokedAt).HasColumnName("revoked_at");
        builder.Property(rt => rt.ReplacedBy).HasColumnName("replaced_by");
        builder.Property(rt => rt.IpAddress).HasColumnName("ip_address");
        builder.Property(rt => rt.UserAgent).HasColumnName("user_agent");
        builder.Property(rt => rt.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_refresh_tokens_user");

        // Self-referential: replaced_by → refresh_tokens.id
        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(rt => rt.ReplacedBy)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_refresh_tokens_replaced_by");

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_refresh_token_hash");

        builder.HasIndex(rt => rt.UserId).HasDatabaseName("idx_refresh_tokens_user");

        // Partial index: only active tokens (revoked_at IS NULL)
        builder.HasIndex(rt => new { rt.UserId, rt.ExpiresAt })
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("idx_refresh_tokens_active");
    }
}
