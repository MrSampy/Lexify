using Lexify.Application.Abstractions;

namespace Lexify.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
