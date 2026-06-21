using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("question_options");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasColumnName("id");

        builder.Property(o => o.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(o => o.OptionText)
            .HasColumnName("option_text")
            .IsRequired();

        builder.Property(o => o.IsCorrect)
            .HasColumnName("is_correct")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(o => o.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        // fk_question_options_question is configured from Question side (QuestionConfiguration.HasMany)

        builder.HasIndex(o => o.QuestionId)
            .HasDatabaseName("idx_question_options_question");

        builder.HasIndex(o => o.QuestionId)
            .HasFilter("is_correct = TRUE")
            .HasDatabaseName("idx_question_options_correct");
    }
}
