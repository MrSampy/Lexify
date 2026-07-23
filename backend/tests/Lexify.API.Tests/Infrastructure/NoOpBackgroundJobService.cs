using Lexify.Application.Abstractions;

namespace Lexify.API.Tests.Infrastructure;

public sealed class NoOpBackgroundJobService : IBackgroundJobService
{
    public void EnqueueGenerateTest(
        Guid testId, Guid userId, Guid[] blockIds,
        string[] questionTypes, int questionCount)
    {
        // intentionally a no-op — tests check status immediately after enqueue
    }

    public void EnqueueWelcomeEmail(string email, string username)
    {
        // intentionally a no-op
    }

    public void EnqueuePasswordResetEmail(string email, string rawToken)
    {
        // intentionally a no-op
    }

    public void EnqueueEmailVerification(string email, string rawToken, string purpose)
    {
        // intentionally a no-op — tests read the issued token straight from the database
    }

    public void EnqueueTwoFactorCode(string email, string code)
    {
        // intentionally a no-op — tests overwrite the code hash in the database with a known value
    }
}
