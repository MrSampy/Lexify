using Lexify.Application.Abstractions;

namespace Lexify.API.Tests.Infrastructure;

public sealed class NoOpBackgroundJobService : IBackgroundJobService
{
    public void EnqueueGenerateTest(
        Guid testId, Guid userId, Guid[] blockIds,
        string[] questionTypes, int questionCount)
    {
        // intentionally a no-op — tests check status immediately after enqueue
    }
}
