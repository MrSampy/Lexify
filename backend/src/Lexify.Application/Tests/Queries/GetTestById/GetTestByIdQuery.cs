using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using MediatR;

namespace Lexify.Application.Tests.Queries.GetTestById;

public sealed record GetTestByIdQuery(Guid TestId, Guid UserId) : IRequest<Result<TestDto>>;
