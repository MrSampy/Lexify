namespace Lexify.Application.Abstractions;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    DateTimeOffset GetExpiry();
}
