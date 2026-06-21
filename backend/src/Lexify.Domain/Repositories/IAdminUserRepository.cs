namespace Lexify.Domain.Repositories;

public interface IAdminUserRepository
{
    Task<(int Total, IReadOnlyList<AdminUserEntry> Items)> GetPagedWithStatsAsync(
        string? role, string? status, string? emailSearch,
        int page, int pageSize, CancellationToken ct = default);
}
