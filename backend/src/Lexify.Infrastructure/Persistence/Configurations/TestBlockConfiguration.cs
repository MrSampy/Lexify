using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexify.Infrastructure.Persistence.Configurations;

public sealed class TestBlockConfiguration : IEntityTypeConfiguration<TestBlock>
{
    public void Configure(EntityTypeBuilder<TestBlock> builder)
    {
        builder.ToTable("test_blocks");

        builder.HasKey(tb => new { tb.TestId, tb.BlockId });

        builder.Property(tb => tb.TestId)
            .HasColumnName("test_id");

        builder.Property(tb => tb.BlockId)
            .HasColumnName("block_id");

        builder.HasOne<Test>()
            .WithMany()
            .HasForeignKey(tb => tb.TestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_blocks_test");

        builder.HasOne<WordBlock>()
            .WithMany()
            .HasForeignKey(tb => tb.BlockId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_blocks_block");

        builder.HasIndex(tb => tb.BlockId)
            .HasDatabaseName("idx_test_blocks_block");
    }
}
