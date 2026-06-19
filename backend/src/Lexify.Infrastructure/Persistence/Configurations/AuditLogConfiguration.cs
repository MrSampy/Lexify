using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.AdminId)
            .HasColumnName("admin_id")
            .IsRequired();

        builder.Property(l => l.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.TargetType)
            .HasColumnName("target_type")
            .HasMaxLength(50);

        builder.Property(l => l.TargetId)
            .HasColumnName("target_id");

        builder.Property(l => l.OldValue)
            .HasColumnName("old_value")
            .HasColumnType("jsonb");

        builder.Property(l => l.NewValue)
            .HasColumnName("new_value")
            .HasColumnType("jsonb");

        builder.Property(l => l.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45); // IPv4 (15) or IPv6 (45)

        builder.Property(l => l.UserAgent)
            .HasColumnName("user_agent");

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // No CASCADE — audit records survive even if admin account is deleted
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.AdminId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_audit_admin");

        builder.HasIndex(l => l.AdminId)
            .HasDatabaseName("idx_audit_admin");

        builder.HasIndex(l => new { l.TargetType, l.TargetId })
            .HasDatabaseName("idx_audit_target");

        builder.HasIndex(l => new { l.Action, l.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_audit_action");

        builder.HasIndex(l => l.CreatedAt)
            .IsDescending()
            .HasDatabaseName("idx_audit_created");
    }
}
