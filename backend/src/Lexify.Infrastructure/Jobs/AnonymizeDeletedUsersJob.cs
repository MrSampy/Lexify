using Lexify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class AnonymizeDeletedUsersJob(
    AppDbContext context,
    ILogger<AnonymizeDeletedUsersJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        await context.Database.ExecuteSqlRawAsync(
            "SELECT fn_anonymize_deleted_users()", ct);
        LogCompleted(logger);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "AnonymizeDeletedUsersJob: anonymization completed")]
    private static partial void LogCompleted(ILogger logger);
}
