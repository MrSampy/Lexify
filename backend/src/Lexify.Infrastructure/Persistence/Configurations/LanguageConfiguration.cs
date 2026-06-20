using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("languages");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(l => l.Code)
            .HasColumnName("code")
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(l => l.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.NativeName)
            .HasColumnName("native_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(l => l.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_languages_code", "code ~ '^[a-z]{2,5}$'"));

        builder.HasIndex(l => l.Code)
            .IsUnique()
            .HasDatabaseName("uq_languages_code");

        builder.HasIndex(l => new { l.IsActive, l.SortOrder })
            .HasDatabaseName("idx_languages_active");
    }
}
