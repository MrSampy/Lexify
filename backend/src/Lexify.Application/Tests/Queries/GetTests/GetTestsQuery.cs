using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using MediatR;

namespace Lexify.Application.Tests.Queries.GetTests;

public sealed record GetTestsQuery(
    Guid UserId,
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<TestListItemDto>>>;
