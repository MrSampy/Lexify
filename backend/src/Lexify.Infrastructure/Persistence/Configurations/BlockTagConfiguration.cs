using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class BlockTagConfiguration : IEntityTypeConfiguration<BlockTag>
{
    public void Configure(EntityTypeBuilder<BlockTag> builder)
    {
        builder.ToTable("block_tags");

        builder.HasKey(bt => new { bt.BlockId, bt.TagId });

        builder.Property(bt => bt.BlockId).HasColumnName("block_id");
        builder.Property(bt => bt.TagId).HasColumnName("tag_id");

        builder.HasOne<WordBlock>()
            .WithMany()
            .HasForeignKey(bt => bt.BlockId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_block_tags_block");

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(bt => bt.TagId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_block_tags_tag");

        builder.HasIndex(bt => bt.TagId).HasDatabaseName("idx_block_tags_tag");
    }
}
