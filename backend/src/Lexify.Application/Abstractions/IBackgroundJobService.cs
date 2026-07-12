namespace Lexify.Application.Abstractions;

public interface IBackgroundJobService
{
    void EnqueueGenerateTest(
        Guid testId,
        Guid userId,
        Guid[] blockIds,
        string[] questionTypes,
        int questionCount);

    void EnqueueWelcomeEmail(string email, string username);

    void EnqueuePasswordResetEmail(string email, string rawToken);
}
