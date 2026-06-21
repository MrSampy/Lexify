using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
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

        var filtered = (string.IsNullOrEmpty(request.Status)
            ? tests
            : tests.Where(t => t.Status == request.Status))
            .ToList();

        var total = filtered.Count;
        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TestListItemDto(t.Id, t.Title, t.Status, t.QuestionCount, t.CreatedAt))
            .ToList();

        return Result.Ok(new PagedResult<TestListItemDto>(items, total, request.Page, request.PageSize));
    }
}
