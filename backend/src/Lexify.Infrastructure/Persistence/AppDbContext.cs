using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<LoginTwoFactorCode> LoginTwoFactorCodes => Set<LoginTwoFactorCode>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<WordBlock> WordBlocks => Set<WordBlock>();
    public DbSet<BlockTag> BlockTags => Set<BlockTag>();
    public DbSet<Word> Words => Set<Word>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestBlock> TestBlocks => Set<TestBlock>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<AiCallLog> AiCallLogs => Set<AiCallLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WordReviewLog> WordReviewLogs => Set<WordReviewLog>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<FeedbackAttachment> FeedbackAttachments => Set<FeedbackAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
