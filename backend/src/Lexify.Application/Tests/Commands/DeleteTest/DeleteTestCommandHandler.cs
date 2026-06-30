using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tests.Commands.DeleteTest;

public sealed class DeleteTestCommandHandler(
    ITestRepository testRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTestCommand, Result>
{
    public async Task<Result> Handle(DeleteTestCommand request, CancellationToken cancellationToken)
    {
        var test = await testRepository.GetByIdAsync(request.TestId, cancellationToken);
        if (test is null)
            return Result.NotFound("Test not found.");

        if (test.UserId != request.UserId)
            return Result.Forbidden("You do not have access to this test.");

        if (!test.IsArchived)
        {
            test.Archive();
            await testRepository.UpdateAsync(test, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }
}
