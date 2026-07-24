using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class BlockShareConfiguration : IEntityTypeConfiguration<BlockShare>
{
    public void Configure(EntityTypeBuilder<BlockShare> builder)
    {
        builder.ToTable("block_shares");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.BlockId).HasColumnName("block_id").IsRequired();
        builder.Property(s => s.OwnerUserId).HasColumnName("owner_user_id").IsRequired();

        builder.Property(s => s.Token)
            .HasColumnName("token")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.RevokedAt).HasColumnName("revoked_at");

        builder.Property(s => s.ViewCount).HasColumnName("view_count").HasDefaultValue(0).IsRequired();
        builder.Property(s => s.CopyCount).HasColumnName("copy_count").HasDefaultValue(0).IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<WordBlock>()
            .WithMany()
            .HasForeignKey(s => s.BlockId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_block_shares_block");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_block_shares_owner");

        // The token is the only credential the link carries, so lookups by it must be unique and indexed.
        builder.HasIndex(s => s.Token).IsUnique().HasDatabaseName("idx_block_shares_token");

        builder.HasIndex(s => s.BlockId).HasDatabaseName("idx_block_shares_block");
    }
}
