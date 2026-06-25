using Lexify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class CleanupAiLogsJob(
    AppDbContext context,
    ILogger<CleanupAiLogsJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-12);
        var deleted = await context.AiCallLogs
            .Where(l => l.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);
        LogCompleted(logger, deleted);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "CleanupAiLogsJob: deleted {Count} AI log entries older than 12 months")]
    private static partial void LogCompleted(ILogger logger, int count);
}
