using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("feedback");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").ValueGeneratedNever();

        // The one DB-generated column: a plain identity sequence, so ticket codes are short and
        // increasing. Read back after SaveChanges — it has no value before the INSERT.
        builder.Property(f => f.TicketNumber)
            .HasColumnName("ticket_number")
            .UseIdentityAlwaysColumn()
            .HasIdentityOptions(startValue: 1000)
            .ValueGeneratedOnAdd();

        builder.Property(f => f.UserId).HasColumnName("user_id");

        builder.Property(f => f.Type)
            .HasColumnName("type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.Category)
            .HasColumnName("category")
            .HasMaxLength(40);

        builder.Property(f => f.Subject)
            .HasColumnName("subject")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(f => f.Message)
            .HasColumnName("message")
            .IsRequired();

        builder.Property(f => f.Rating).HasColumnName("rating");

        builder.Property(f => f.ContactEmail)
            .HasColumnName("contact_email")
            .HasMaxLength(320);

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(Feedback.Statuses.New)
            .IsRequired();

        builder.Property(f => f.AdminNote).HasColumnName("admin_note");
        builder.Property(f => f.ResolvedBy).HasColumnName("resolved_by");
        builder.Property(f => f.ResolvedAt).HasColumnName("resolved_at");

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // SET NULL, not cascade: a deleted account must not take its bug reports with it — the
        // report is still actionable, it just loses its author.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_feedback_user");

        builder.HasMany(f => f.Attachments)
            .WithOne()
            .HasForeignKey(a => a.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_feedback_attachments_feedback");

        builder.Metadata
            .FindNavigation(nameof(Feedback.Attachments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "chk_feedback_type", "type IN ('suggestion', 'bug', 'review', 'question')");
            t.HasCheckConstraint(
                "chk_feedback_status", "status IN ('new', 'in_progress', 'resolved')");
            // Mirrors the invariant in Feedback.Create: a rating belongs to a review and nothing else.
            t.HasCheckConstraint(
                "chk_feedback_rating",
                "(type = 'review' AND rating BETWEEN 1 AND 5) OR (type <> 'review' AND rating IS NULL)");
        });

        builder.HasIndex(f => f.TicketNumber)
            .IsUnique()
            .HasDatabaseName("idx_feedback_ticket_number");

        builder.HasIndex(f => f.UserId).HasDatabaseName("idx_feedback_user_id");

        // The triage list's default ordering: newest open items first.
        builder.HasIndex(f => new { f.Status, f.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_feedback_status_created");
    }
}
