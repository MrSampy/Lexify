using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .HasDefaultValue(User.Roles.User)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(User.Statuses.Active)
            .IsRequired();

        builder.Property(u => u.EnglishLevel)
            .HasColumnName("english_level")
            .HasMaxLength(2);

        builder.Property(u => u.NewWordsPerDay)
            .HasColumnName("new_words_per_day")
            .HasDefaultValue(User.DefaultNewWordsPerDay)
            .IsRequired();

        builder.Property(u => u.EmailRemindersEnabled)
            .HasColumnName("email_reminders_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(u => u.EmailVerifiedAt).HasColumnName("email_verified_at");
        builder.Property(u => u.TwoFactorEnabled)
            .HasColumnName("two_factor_enabled")
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property(u => u.LastActiveAt).HasColumnName("last_active_at");
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_users_role", "role IN ('user', 'moderator', 'admin')");
            t.HasCheckConstraint("chk_users_status", "status IN ('active', 'suspended', 'deleted')");
            t.HasCheckConstraint("chk_users_english_level",
                "english_level IS NULL OR english_level IN ('A1', 'A2', 'B1', 'B2', 'C1', 'C2')");
            t.HasCheckConstraint("chk_users_new_words",
                "new_words_per_day >= 0 AND new_words_per_day <= 100");
        });

        // Functional unique index LOWER(email) is added via raw SQL in the migration
        builder.HasIndex(u => u.Role).HasDatabaseName("idx_users_role");

        builder.HasIndex(u => u.Status)
            .HasFilter("status != 'active'")
            .HasDatabaseName("idx_users_status");

        builder.HasIndex(u => u.LastActiveAt)
            .IsDescending()
            .HasDatabaseName("idx_users_last_active");
    }
}
