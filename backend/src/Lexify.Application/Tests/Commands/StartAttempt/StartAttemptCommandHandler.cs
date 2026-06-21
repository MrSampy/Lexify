using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tests.Commands.StartAttempt;

public sealed class StartAttemptCommandHandler(
    ITestRepository testRepository,
    ITestAttemptRepository attemptRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<StartAttemptCommand, Result<StartAttemptResult>>
{
    public async Task<Result<StartAttemptResult>> Handle(
        StartAttemptCommand request, CancellationToken cancellationToken)
    {
        var test = await testRepository.GetByIdAsync(request.TestId, cancellationToken);
        if (test is null)
            return Result.NotFound<StartAttemptResult>("Test not found.");

        if (test.UserId != request.UserId)
            return Result.Forbidden<StartAttemptResult>("You do not have access to this test.");

        if (test.Status != Test.Statuses.Ready)
            return Result.Failure<StartAttemptResult>(
                $"Test is not ready for attempts (current status: {test.Status}).");

        var attempt = TestAttempt.Start(request.TestId, request.UserId);
        await attemptRepository.AddAsync(attempt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new StartAttemptResult(attempt.Id));
    }
}
