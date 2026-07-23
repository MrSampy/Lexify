using Lexify.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class CleanupEmailVerificationTokensJob(
    IEmailVerificationTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ILogger<CleanupEmailVerificationTokensJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var deleted = await tokenRepository.DeleteExpiredAsync(ct);
        await unitOfWork.SaveChangesAsync(ct);
        LogCompleted(logger, deleted);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "CleanupEmailVerificationTokensJob: deleted {Count} expired/used tokens")]
    private static partial void LogCompleted(ILogger logger, int count);
}
