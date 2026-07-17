using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetAccuracy;

/// <param name="Days">Window size in days back from today (clamped 1–366).</param>
public sealed record GetAccuracyQuery(Guid UserId, int Days = 30)
    : IRequest<Result<AccuracyDto>>;

/// <param name="Correct">Reviews graded as recalled (SM-2 quality &gt;= 3).</param>
public sealed record DailyAccuracy(DateOnly Date, int Total, int Correct);

public sealed record AccuracyDto(IReadOnlyList<DailyAccuracy> Days);
