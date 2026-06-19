using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");

        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Value)
            .HasColumnName("value")
            .IsRequired();

        builder.Property(s => s.ValueType)
            .HasColumnName("value_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasColumnName("description");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // FK to users.id — added via raw SQL in migration 1.3 when users table is created
        builder.Property(s => s.UpdatedBy)
            .HasColumnName("updated_by");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_settings_type", "value_type IN ('string', 'bool', 'int', 'json')"));
    }
}
