namespace Lexify.Domain.Repositories;

public interface IAdminStatsRepository
{
    Task<int> CountUsersAsync(CancellationToken ct = default);
    Task<int> CountActiveUsersAsync(DateTimeOffset since, CancellationToken ct = default);
    Task<int> CountWordsAsync(CancellationToken ct = default);
    Task<int> CountWordBlocksAsync(CancellationToken ct = default);
    Task<int> CountTestsAsync(CancellationToken ct = default);
    Task<int> CountAiCallsAsync(DateTimeOffset since, CancellationToken ct = default);

    Task<IReadOnlyList<(string Code, string Name, int BlockCount)>> GetTopLanguagesByBlockCountAsync(
        int top, CancellationToken ct = default);

    Task<IReadOnlyList<(DateOnly Date, int Count)>> GetRegistrationsByDayAsync(
        int days, CancellationToken ct = default);

    Task<IReadOnlyList<(DateTimeOffset HourStart, int Count)>> GetAiCallsByHourAsync(
        int hours, CancellationToken ct = default);
}
