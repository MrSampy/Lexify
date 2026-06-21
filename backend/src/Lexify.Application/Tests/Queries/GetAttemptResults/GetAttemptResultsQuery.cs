using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using MediatR;

namespace Lexify.Application.Tests.Queries.GetAttemptResults;

public sealed record GetAttemptResultsQuery(Guid AttemptId, Guid UserId) : IRequest<Result<AttemptResultDto>>;
