using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class WordReviewLogConfiguration : IEntityTypeConfiguration<WordReviewLog>
{
    public void Configure(EntityTypeBuilder<WordReviewLog> builder)
    {
        builder.ToTable("word_review_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property(l => l.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(l => l.WordId).HasColumnName("word_id").IsRequired();
        builder.Property(l => l.BlockId).HasColumnName("block_id").IsRequired();
        builder.Property(l => l.LanguageId).HasColumnName("language_id").IsRequired();
        builder.Property(l => l.Quality).HasColumnName("quality").IsRequired();

        builder.Property(l => l.Source)
            .HasColumnName("source")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.EaseFactorAfter).HasColumnName("ease_factor_after").IsRequired();
        builder.Property(l => l.IntervalDaysAfter).HasColumnName("interval_days_after").IsRequired();
        builder.Property(l => l.ReviewedAt).HasColumnName("reviewed_at").IsRequired();

        // Reviews outlive the words/blocks they describe: keep history even if the word is deleted, so
        // set the FK to a no-action (history rows are never joined back for integrity, only aggregated).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_review_logs_user");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_review_logs_quality", "quality BETWEEN 0 AND 5");
            t.HasCheckConstraint("chk_review_logs_source", "source IN ('review', 'test', 'conversation')");
        });

        // Serves every stats query: "this user's reviews since T", newest-relevant first.
        builder.HasIndex(l => new { l.UserId, l.ReviewedAt })
            .HasDatabaseName("idx_review_logs_user_time");

        // Serves per-word history and "was this word ever reviewed before?" lookups.
        builder.HasIndex(l => new { l.WordId, l.ReviewedAt })
            .HasDatabaseName("idx_review_logs_word_time");
    }
}
