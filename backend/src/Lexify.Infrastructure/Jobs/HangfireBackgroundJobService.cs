using Hangfire;
using Lexify.Application.Abstractions;

namespace Lexify.Infrastructure.Jobs;

public sealed class HangfireBackgroundJobService(IBackgroundJobClient client) : IBackgroundJobService
{
    public void EnqueueGenerateTest(
        Guid testId,
        Guid userId,
        Guid[] blockIds,
        string[] questionTypes,
        int questionCount)
    {
        client.Enqueue<GenerateTestJob>(job =>
            job.RunAsync(testId, userId, blockIds, questionTypes, questionCount, CancellationToken.None));
    }
}
