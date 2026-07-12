using Lexify.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class CleanupPasswordResetTokensJob(
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IUnitOfWork unitOfWork,
    ILogger<CleanupPasswordResetTokensJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var deleted = await passwordResetTokenRepository.DeleteExpiredAsync(ct);
        await unitOfWork.SaveChangesAsync(ct);
        LogCompleted(logger, deleted);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "CleanupPasswordResetTokensJob: deleted {Count} expired/used tokens")]
    private static partial void LogCompleted(ILogger logger, int count);
}
