using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class AiCallLogConfiguration : IEntityTypeConfiguration<AiCallLog>
{
    public void Configure(EntityTypeBuilder<AiCallLog> builder)
    {
        builder.ToTable("ai_call_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.UserId)
            .HasColumnName("user_id");

        builder.Property(l => l.CallType)
            .HasColumnName("call_type")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(l => l.Provider)
            .HasColumnName("provider")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.Model)
            .HasColumnName("model")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.InputTokens)
            .HasColumnName("input_tokens");

        builder.Property(l => l.OutputTokens)
            .HasColumnName("output_tokens");

        builder.Property(l => l.DurationMs)
            .HasColumnName("duration_ms")
            .IsRequired();

        builder.Property(l => l.Success)
            .HasColumnName("success")
            .IsRequired();

        builder.Property(l => l.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(l => l.InputHash)
            .HasColumnName("input_hash")
            .HasMaxLength(64);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_ai_logs_user");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_ai_logs_provider",
                "provider IN ('ollama', 'openai')");
            t.HasCheckConstraint("chk_ai_logs_type",
                "call_type IN ('format_words', 'generate_test', 'suggest_title')");
            t.HasCheckConstraint("chk_ai_logs_duration",
                "duration_ms >= 0");
        });

        builder.HasIndex(l => l.CreatedAt)
            .IsDescending()
            .HasDatabaseName("idx_ai_logs_created");

        builder.HasIndex(l => l.UserId)
            .HasFilter("user_id IS NOT NULL")
            .HasDatabaseName("idx_ai_logs_user");

        builder.HasIndex(l => new { l.Provider, l.Success })
            .HasDatabaseName("idx_ai_logs_provider");

        builder.HasIndex(l => new { l.CallType, l.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_ai_logs_type");

        builder.HasIndex(l => l.CreatedAt)
            .IsDescending()
            .HasFilter("success = FALSE")
            .HasDatabaseName("idx_ai_logs_errors");
    }
}
