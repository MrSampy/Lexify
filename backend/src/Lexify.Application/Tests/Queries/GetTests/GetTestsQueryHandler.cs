using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tests.Queries.GetTests;

public sealed class GetTestsQueryHandler(
    ITestRepository testRepository)
    : IRequestHandler<GetTestsQuery, Result<PagedResult<TestListItemDto>>>
{
    public async Task<Result<PagedResult<TestListItemDto>>> Handle(
        GetTestsQuery request, CancellationToken cancellationToken)
    {
        var tests = await testRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        // "Delete" on a test just archives it (preserves question-dedup history and past attempts —
        // see DeleteTestCommandHandler). Without an explicit status filter, archived tests must stay
        // hidden or "deleting" a test wouldn't actually remove it from what the user sees.
        var filtered = (request.Status switch
        {
            null or "" => tests.Where(t => t.Status != Test.Statuses.Archived),
            _ => tests.Where(t => t.Status == request.Status)
        }).ToList();

        var total = filtered.Count;
        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TestListItemDto(t.Id, t.Title, t.Status, t.QuestionCount, t.CreatedAt))
            .ToList();

        return Result.Ok(new PagedResult<TestListItemDto>(items, total, request.Page, request.PageSize));
    }
}
