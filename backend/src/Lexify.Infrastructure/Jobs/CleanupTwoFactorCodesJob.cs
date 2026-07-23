using Lexify.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class CleanupTwoFactorCodesJob(
    ILoginTwoFactorCodeRepository codeRepository,
    IUnitOfWork unitOfWork,
    ILogger<CleanupTwoFactorCodesJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var deleted = await codeRepository.DeleteExpiredAsync(ct);
        await unitOfWork.SaveChangesAsync(ct);
        LogCompleted(logger, deleted);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "CleanupTwoFactorCodesJob: deleted {Count} expired/used codes")]
    private static partial void LogCompleted(ILogger logger, int count);
}
