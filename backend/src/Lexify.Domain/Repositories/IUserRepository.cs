using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    /// <summary>Returns email + due word count for all active users who have at least 1 due word.</summary>
    Task<IReadOnlyList<(string Email, int DueCount)>> GetUsersWithDueWordsAsync(CancellationToken ct = default);
}
