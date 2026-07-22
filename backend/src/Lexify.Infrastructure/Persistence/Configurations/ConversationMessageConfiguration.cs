using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("conversation_messages");

        builder.HasKey(m => m.Id);
        // Client-assigned Guid (set in the constructor). Without this, EF's graph-add logic sees a
        // non-default key on a message added to an already-tracked Conversation and assumes the row
        // exists — issuing an UPDATE (0 rows → concurrency error) instead of an INSERT.
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.ConversationId).HasColumnName("conversation_id").IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(m => m.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_conversation_messages_role", "role IN ('user', 'assistant')"));

        builder.HasIndex(m => new { m.ConversationId, m.SortOrder })
            .HasDatabaseName("idx_conversation_messages_order");
    }
}
