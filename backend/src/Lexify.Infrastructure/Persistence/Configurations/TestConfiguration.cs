using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class TestConfiguration : IEntityTypeConfiguration<Test>
{
    public void Configure(EntityTypeBuilder<Test> builder)
    {
        builder.ToTable("tests");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("generating")
            .IsRequired();

        builder.Property(t => t.QuestionCount)
            .HasColumnName("question_count");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Ignore(t => t.UpdatedAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_tests_user");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_tests_status", "status IN ('generating', 'ready', 'archived')"));

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("idx_tests_user_id");

        builder.HasIndex(t => new { t.UserId, t.Status })
            .HasDatabaseName("idx_tests_status");

        builder.HasIndex(t => new { t.UserId, t.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_tests_created");
    }
}
