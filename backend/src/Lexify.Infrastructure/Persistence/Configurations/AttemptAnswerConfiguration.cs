using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
    {
        builder.ToTable("attempt_answers");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.AttemptId)
            .HasColumnName("attempt_id")
            .IsRequired();

        builder.Property(a => a.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(a => a.GivenAnswer)
            .HasColumnName("given_answer")
            .IsRequired();

        builder.Property(a => a.IsCorrect)
            .HasColumnName("is_correct")
            .IsRequired();

        builder.Property(a => a.TimeSpentMs)
            .HasColumnName("time_spent_ms");

        builder.Property(a => a.AnsweredAt)
            .HasColumnName("answered_at")
            .IsRequired();

        // fk_answers_attempt is configured from TestAttempt side (TestAttemptConfiguration.HasMany)
        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_answers_question");

        builder.HasIndex(a => new { a.AttemptId, a.QuestionId })
            .IsUnique()
            .HasDatabaseName("uq_attempt_question");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_answers_time", "time_spent_ms IS NULL OR time_spent_ms >= 0"));

        builder.HasIndex(a => a.AttemptId)
            .HasDatabaseName("idx_attempt_answers_attempt");

        builder.HasIndex(a => a.QuestionId)
            .HasDatabaseName("idx_attempt_answers_question");

        builder.HasIndex(a => a.AttemptId)
            .HasFilter("is_correct = FALSE")
            .HasDatabaseName("idx_attempt_answers_wrong");
    }
}
