using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class LanguageRepository(AppDbContext context) : ILanguageRepository
{
    public async Task<IReadOnlyList<Language>> GetAllAsync(
        bool includeInactive = false, CancellationToken ct = default)
    {
        var query = context.Languages.AsNoTracking();
        if (!includeInactive)
            query = query.Where(l => l.IsActive);
        return await query.OrderBy(l => l.SortOrder).ThenBy(l => l.Name).ToListAsync(ct);
    }

    public Task<Language?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        context.Languages.FirstOrDefaultAsync(l => l.Code == code, ct);

    public Task<Language?> GetByIdAsync(short id, CancellationToken ct = default) =>
        context.Languages.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task AddAsync(Language language, CancellationToken ct = default) =>
        await context.Languages.AddAsync(language, ct);

    public Task UpdateAsync(Language language, CancellationToken ct = default)
    {
        context.Languages.Update(language);
        return Task.CompletedTask;
    }
}
