using Lexify.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class CleanupRefreshTokensJob(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ILogger<CleanupRefreshTokensJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var deleted = await refreshTokenRepository.DeleteExpiredAsync(ct);
        await unitOfWork.SaveChangesAsync(ct);
        LogCompleted(logger, deleted);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "CleanupRefreshTokensJob: deleted {Count} expired tokens")]
    private static partial void LogCompleted(ILogger logger, int count);
}
