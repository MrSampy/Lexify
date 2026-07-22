using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.LanguageId).HasColumnName("language_id").IsRequired();

        builder.Property(c => c.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Scenario)
            .HasColumnName("scenario")
            .HasMaxLength(200);

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(Conversation.Statuses.Active)
            .IsRequired();

        // Backing field for the read-only TargetWordIds collection; stored as a native uuid[] column.
        builder.Property<List<Guid>>("_targetWordIds")
            .HasColumnName("target_word_ids")
            .HasColumnType("uuid[]")
            .HasDefaultValueSql("'{}'::uuid[]")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Ignore(c => c.TargetWordIds);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.EndedAt).HasColumnName("ended_at");

        // Final score, recorded at End; null for conversations ended before scoring was persisted.
        builder.Property(c => c.Points).HasColumnName("points");
        builder.Property(c => c.Stars).HasColumnName("stars");
        builder.Property(c => c.WordsUsed).HasColumnName("words_used");
        builder.Ignore(c => c.UpdatedAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_conversations_user");

        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_conversation_messages_conversation");

        builder.Metadata
            .FindNavigation(nameof(Conversation.Messages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_conversations_status", "status IN ('active', 'ended')"));

        builder.HasIndex(c => c.UserId).HasDatabaseName("idx_conversations_user_id");

        builder.HasIndex(c => new { c.UserId, c.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_conversations_created");
    }
}
