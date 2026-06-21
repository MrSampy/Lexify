namespace Lexify.Application.Abstractions;

public interface IBackgroundJobService
{
    void EnqueueGenerateTest(
        Guid testId,
        Guid userId,
        Guid[] blockIds,
        string[] questionTypes,
        int questionCount);
}
