namespace Lexify.Application.Abstractions;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateImpersonationToken(Guid targetUserId, string targetEmail, string targetRole, Guid adminId);
    DateTimeOffset GetExpiry();

    /// <summary>
    /// A short-lived token asserting "user X passed the password step and owes a 2FA code". Signed with
    /// the same key but a distinct audience/purpose, so the bearer pipeline rejects it against every
    /// [Authorize] endpoint — it only unlocks the code-verification step.
    /// </summary>
    string GenerateTwoFactorChallengeToken(Guid userId);

    /// <summary>Returns the subject user id if the challenge token is valid and unexpired; null otherwise.</summary>
    Task<Guid?> ValidateTwoFactorChallengeToken(string token);
}
