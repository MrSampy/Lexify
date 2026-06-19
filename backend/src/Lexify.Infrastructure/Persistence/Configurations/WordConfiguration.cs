using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.ToTable("words");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");

        builder.Property(w => w.BlockId).HasColumnName("block_id").IsRequired();
        builder.Property(w => w.Term).HasColumnName("term").IsRequired();
        builder.Property(w => w.Translation).HasColumnName("translation").IsRequired();

        builder.Property(w => w.WordType)
            .HasColumnName("word_type")
            .HasMaxLength(20)
            .HasDefaultValue(Word.WordTypes.Word)
            .IsRequired();

        builder.Property(w => w.Notes).HasColumnName("notes");
        builder.Property(w => w.ExampleSentence).HasColumnName("example_sentence");

        builder.Property(w => w.ConfidenceFlag)
            .HasColumnName("confidence_flag")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(w => w.ConfidenceNote).HasColumnName("confidence_note");

        builder.Property(w => w.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(w => w.EaseFactor)
            .HasColumnName("ease_factor")
            .HasDefaultValue(2.5)
            .IsRequired();

        builder.Property(w => w.IntervalDays)
            .HasColumnName("interval_days")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(w => w.Repetitions)
            .HasColumnName("repetitions")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(w => w.NextReviewAt)
            .HasColumnName("next_review_at")
            .IsRequired();

        builder.HasOne<WordBlock>()
            .WithMany()
            .HasForeignKey(w => w.BlockId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_words_block");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_words_type",
                "word_type IN ('word', 'phrase', 'idiom', 'expression')");
            t.HasCheckConstraint("chk_words_ease", "ease_factor >= 1.3");
            t.HasCheckConstraint("chk_words_interval", "interval_days >= 1");
            t.HasCheckConstraint("chk_words_reps", "repetitions >= 0");
            t.HasCheckConstraint("chk_words_term", "LENGTH(TRIM(term)) > 0");
            t.HasCheckConstraint("chk_words_trans", "LENGTH(TRIM(translation)) > 0");
        });

        builder.HasIndex(w => w.BlockId).HasDatabaseName("idx_words_block_id");

        builder.HasIndex(w => new { w.BlockId, w.SortOrder })
            .HasDatabaseName("idx_words_sort");

        builder.HasIndex(w => new { w.BlockId, w.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_words_created");

        // Partial index for SM-2: only words due for review
        builder.HasIndex(w => w.NextReviewAt)
            .HasFilter("next_review_at <= NOW()")
            .HasDatabaseName("idx_words_due_review");

        // Partial index for confidence flag filtering
        builder.HasIndex(w => w.BlockId)
            .HasFilter("confidence_flag = TRUE")
            .HasDatabaseName("idx_words_confidence");

        // GIN trigram indexes for ILIKE search (pg_trgm)
        builder.HasIndex(w => w.Term)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("idx_words_term_trgm");

        builder.HasIndex(w => w.Translation)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("idx_words_translation_trgm");

        // Full-text search GIN index (to_tsvector + unaccent) is added via raw SQL in migration
    }
}
