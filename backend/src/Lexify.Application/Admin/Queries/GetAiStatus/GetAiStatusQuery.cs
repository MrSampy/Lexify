using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAiStatus;

public sealed record GetAiStatusQuery(int WindowMinutes = 60)
    : IRequest<Result<IReadOnlyList<AiProviderStatusDto>>>;
