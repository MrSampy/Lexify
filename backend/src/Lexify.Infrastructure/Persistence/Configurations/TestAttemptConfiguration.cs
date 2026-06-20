using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class TestAttemptConfiguration : IEntityTypeConfiguration<TestAttempt>
{
    public void Configure(EntityTypeBuilder<TestAttempt> builder)
    {
        builder.ToTable("test_attempts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.TestId)
            .HasColumnName("test_id")
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(a => a.FinishedAt)
            .HasColumnName("finished_at");

        builder.Property(a => a.Score)
            .HasColumnName("score");

        builder.Property(a => a.TotalQuestions)
            .HasColumnName("total_questions");

        builder.Property(a => a.CorrectAnswers)
            .HasColumnName("correct_answers");

        builder.HasOne<Test>()
            .WithMany()
            .HasForeignKey(a => a.TestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_attempts_test");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_attempts_user");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_attempts_score",
                "score IS NULL OR (score >= 0.0 AND score <= 1.0)");
            t.HasCheckConstraint("chk_attempts_counts",
                "(total_questions IS NULL AND correct_answers IS NULL) OR " +
                "(total_questions >= 0 AND correct_answers >= 0 AND correct_answers <= total_questions)");
        });

        builder.HasIndex(a => a.TestId)
            .HasDatabaseName("idx_attempts_test_id");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("idx_attempts_user_id");

        builder.HasIndex(a => new { a.UserId, a.StartedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_attempts_started");

        builder.HasIndex(a => a.UserId)
            .HasFilter("finished_at IS NULL")
            .HasDatabaseName("idx_attempts_incomplete");
    }
}
