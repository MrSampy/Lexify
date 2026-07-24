using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Stamps <see cref="User.LastActiveAt"/> with the current time. Deliberately a single-column write
    /// rather than a tracked entity update: it runs on ordinary API traffic (see LastActiveMiddleware),
    /// must not load the user, and must not disturb <c>UpdatedAt</c> — activity is not a profile edit.
    /// </summary>
    Task TouchLastActiveAsync(Guid userId, CancellationToken ct = default);
    /// <summary>
    /// Returns id + email + due word count for every active user who has at least 1 due word and has
    /// not opted out of reminder emails.
    /// </summary>
    Task<IReadOnlyList<(Guid UserId, string Email, int DueCount)>> GetUsersWithDueWordsAsync(
        CancellationToken ct = default);
}
