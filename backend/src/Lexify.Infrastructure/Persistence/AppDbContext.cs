using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<WordBlock> WordBlocks => Set<WordBlock>();
    public DbSet<BlockTag> BlockTags => Set<BlockTag>();
    public DbSet<Word> Words => Set<Word>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
