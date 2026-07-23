using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class LoginTwoFactorCodeConfiguration
    : IEntityTypeConfiguration<LoginTwoFactorCode>
{
    public void Configure(EntityTypeBuilder<LoginTwoFactorCode> builder)
    {
        builder.ToTable("login_two_factor_codes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();

        // SHA-256 hex — same width as the other token hashes.
        builder.Property(c => c.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(c => c.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(c => c.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(c => c.UsedAt).HasColumnName("used_at");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_login_two_factor_codes_user");

        builder.ToTable(t =>
            t.HasCheckConstraint("chk_login_two_factor_codes_attempts", "attempts >= 0"));

        // No unique index on code_hash: two users can independently draw the same 6-digit code (same hash),
        // and every lookup is scoped by user_id anyway.
        builder.HasIndex(c => c.UserId).HasDatabaseName("idx_login_two_factor_codes_user");
    }
}
