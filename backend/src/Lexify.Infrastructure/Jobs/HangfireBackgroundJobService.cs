using Hangfire;
using Lexify.Application.Abstractions;

namespace Lexify.Infrastructure.Jobs;

public sealed class HangfireBackgroundJobService(IBackgroundJobClient client) : IBackgroundJobService
{
    public void EnqueueGenerateTest(
        Guid testId,
        Guid userId,
        Guid[] blockIds,
        string[] questionTypes,
        int questionCount)
    {
        client.Enqueue<GenerateTestJob>(job =>
            job.RunAsync(testId, userId, blockIds, questionTypes, questionCount, CancellationToken.None));
    }

    public void EnqueueWelcomeEmail(string email, string username)
    {
        client.Enqueue<SendWelcomeEmailJob>(job =>
            job.RunAsync(email, username, CancellationToken.None));
    }

    public void EnqueuePasswordResetEmail(string email, string rawToken)
    {
        client.Enqueue<SendPasswordResetEmailJob>(job =>
            job.RunAsync(email, rawToken, CancellationToken.None));
    }

    public void EnqueueEmailVerification(string email, string rawToken, string purpose)
    {
        client.Enqueue<SendEmailVerificationJob>(job =>
            job.RunAsync(email, rawToken, purpose, CancellationToken.None));
    }

    public void EnqueueTwoFactorCode(string email, string code)
    {
        client.Enqueue<Send2faCodeJob>(job =>
            job.RunAsync(email, code, CancellationToken.None));
    }

    public void EnqueueEmailChangedNotice(string oldEmail, string newEmail)
    {
        client.Enqueue<SendEmailChangedNoticeJob>(job =>
            job.RunAsync(oldEmail, newEmail, CancellationToken.None));
    }
}
