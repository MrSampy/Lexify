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

    /// <param name="purpose">Signup vs email change — decides the wording of the email.</param>
    void EnqueueEmailVerification(string email, string rawToken, string purpose);

    /// <summary>Sends the one-time sign-in code for two-factor authentication.</summary>
    void EnqueueTwoFactorCode(string email, string code);
}
