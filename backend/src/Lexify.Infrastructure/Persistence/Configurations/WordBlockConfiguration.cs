using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class WordBlockConfiguration : IEntityTypeConfiguration<WordBlock>
{
    public void Configure(EntityTypeBuilder<WordBlock> builder)
    {
        builder.ToTable("word_blocks");

        builder.HasKey(wb => wb.Id);
        builder.Property(wb => wb.Id).HasColumnName("id");

        builder.Property(wb => wb.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(wb => wb.LanguageId).HasColumnName("language_id").IsRequired();

        builder.Property(wb => wb.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(wb => wb.Description).HasColumnName("description");

        builder.Property(wb => wb.WordCount)
            .HasColumnName("word_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(wb => wb.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(wb => wb.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(wb => wb.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_word_blocks_user");

        builder.HasOne<Language>()
            .WithMany()
            .HasForeignKey(wb => wb.LanguageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_word_blocks_language");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_word_blocks_title", "LENGTH(TRIM(title)) > 0");
            t.HasCheckConstraint("chk_word_blocks_count", "word_count >= 0");
        });

        builder.HasIndex(wb => wb.UserId).HasDatabaseName("idx_word_blocks_user_id");

        builder.HasIndex(wb => new { wb.UserId, wb.LanguageId })
            .HasDatabaseName("idx_word_blocks_language");

        builder.HasIndex(wb => new { wb.UserId, wb.UpdatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_word_blocks_updated");
    }
}
