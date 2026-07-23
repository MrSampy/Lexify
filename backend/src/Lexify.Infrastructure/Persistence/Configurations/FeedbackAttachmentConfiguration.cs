using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class FeedbackAttachmentConfiguration : IEntityTypeConfiguration<FeedbackAttachment>
{
    public void Configure(EntityTypeBuilder<FeedbackAttachment> builder)
    {
        builder.ToTable("feedback_attachments");

        builder.HasKey(a => a.Id);
        // Client-assigned Guid — same reason as ConversationMessage: EF must INSERT, not UPDATE.
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.FeedbackId).HasColumnName("feedback_id").IsRequired();

        // The user's original name — display only. It never touches a filesystem path.
        builder.Property(a => a.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.SizeBytes).HasColumnName("size_bytes").IsRequired();

        builder.Property(a => a.StorageName)
            .HasColumnName("storage_name")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(a => a.FeedbackId).HasDatabaseName("idx_feedback_attachments_feedback_id");
    }
}
