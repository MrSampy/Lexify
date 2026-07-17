using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetMastery;

public sealed record GetMasteryQuery(Guid UserId) : IRequest<Result<MasteryDto>>;

public sealed record MasteryDto(int New, int Learning, int Young, int Mature);
