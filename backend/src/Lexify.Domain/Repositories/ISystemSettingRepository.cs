using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface ISystemSettingRepository
{
    Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken ct = default);
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task UpdateAsync(SystemSetting setting, CancellationToken ct = default);
}
