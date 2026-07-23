using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class EmailVerificationTokenConfiguration
    : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("email_verification_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();

        // SHA-256 hex — same width as password_reset_tokens.token_hash.
        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.Purpose)
            .HasColumnName("purpose")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.NewEmail)
            .HasColumnName("new_email")
            .HasMaxLength(320);

        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(t => t.UsedAt).HasColumnName("used_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_email_verification_tokens_user");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "chk_email_verification_tokens_purpose", "purpose IN ('signup', 'email_change')");
            // Mirrors the invariant in the constructor: the address rides along with an email change
            // and only with an email change.
            t.HasCheckConstraint(
                "chk_email_verification_tokens_new_email",
                "(purpose = 'email_change' AND new_email IS NOT NULL) OR (purpose = 'signup' AND new_email IS NULL)");
        });

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_email_verification_token_hash");

        builder.HasIndex(t => t.UserId).HasDatabaseName("idx_email_verification_tokens_user");
    }
}
