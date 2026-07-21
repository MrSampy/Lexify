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
            // Provider is a free-form name from the "AiProviders" config list (Lemonade, Ollama,
            // OpenAI, ...), not a fixed enum — only guard against empty values.
            t.HasCheckConstraint("chk_ai_logs_provider",
                "LENGTH(TRIM(provider)) > 0");
            // 'generate_test' is kept for backward compatibility with historical log rows even
            // though nothing writes it anymore (test generation no longer makes a single whole-test
            // AI call — see generate_fill_sentences/generate_distractors).
            t.HasCheckConstraint("chk_ai_logs_type",
                "call_type IN ('format_words', 'generate_test', 'generate_fill_sentences', 'generate_distractors', 'generate_definitions', 'suggest_title')");
            t.HasCheckConstraint("chk_ai_logs_duration",
                "duration_ms >= 0");
        });

        builder.HasIndex(l => l.CreatedAt)
            .IsDescending()
            .HasDatabaseName("idx_ai_logs_created");

        builder.HasIndex(l => l.UserId)
            .HasFilter("user_id IS NOT NULL")
            .HasDatabaseName("idx_ai_logs_user");

        // Serves the per-user daily quota count (AiQuotaService), which runs on every AI call —
        // without it that count degrades to a seq scan over the whole log table.
        builder.HasIndex(l => new { l.UserId, l.CreatedAt })
            .IsDescending(false, true)
            .HasFilter("user_id IS NOT NULL")
            .HasDatabaseName("idx_ai_logs_user_created");

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
