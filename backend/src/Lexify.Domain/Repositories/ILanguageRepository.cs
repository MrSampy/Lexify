using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface ILanguageRepository
{
    Task<IReadOnlyList<Language>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Language?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(Language language, CancellationToken ct = default);
    Task UpdateAsync(Language language, CancellationToken ct = default);
}
