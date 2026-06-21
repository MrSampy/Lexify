namespace Lexify.Application.Abstractions;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateImpersonationToken(Guid targetUserId, string targetEmail, string targetRole, Guid adminId);
    DateTimeOffset GetExpiry();
}
