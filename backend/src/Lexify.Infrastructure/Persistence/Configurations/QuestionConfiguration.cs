using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id)
            .HasColumnName("id");

        builder.Property(q => q.TestId)
            .HasColumnName("test_id")
            .IsRequired();

        builder.Property(q => q.WordId)
            .HasColumnName("word_id");

        builder.Property(q => q.QuestionType)
            .HasColumnName("question_type")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(q => q.QuestionText)
            .HasColumnName("question_text")
            .IsRequired();

        builder.Property(q => q.CorrectAnswer)
            .HasColumnName("correct_answer")
            .IsRequired();

        builder.Property(q => q.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(q => q.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne<Test>()
            .WithMany()
            .HasForeignKey(q => q.TestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_questions_test");

        builder.HasOne<Word>()
            .WithMany()
            .HasForeignKey(q => q.WordId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_questions_word");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_questions_type",
            "question_type IN ('translate_to_native', 'translate_to_foreign', " +
            "'fill_in_sentence', 'multi_select_theme', 'open_answer')"));

        builder.HasIndex(q => q.TestId)
            .HasDatabaseName("idx_questions_test_id");

        builder.HasIndex(q => q.WordId)
            .HasFilter("word_id IS NOT NULL")
            .HasDatabaseName("idx_questions_word_id");

        builder.HasIndex(q => q.ContentHash)
            .HasDatabaseName("idx_questions_hash");

        builder.HasIndex(q => new { q.TestId, q.SortOrder })
            .HasDatabaseName("idx_questions_sort");
    }
}
