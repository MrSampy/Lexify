using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class SystemSettingRepository(AppDbContext context) : ISystemSettingRepository
{
    public Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken ct = default) =>
        context.SystemSettings
            .AsNoTracking()
            .OrderBy(s => s.Key)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<SystemSetting>)t.Result, ct);

    public Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, ct);

    public Task UpdateAsync(SystemSetting setting, CancellationToken ct = default)
    {
        context.SystemSettings.Update(setting);
        return Task.CompletedTask;
    }
}
