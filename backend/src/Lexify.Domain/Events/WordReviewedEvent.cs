using Lexify.Domain.Common;

namespace Lexify.Domain.Events;

public sealed record WordReviewedEvent(
    Guid WordId,
    int Quality,
    double NewEaseFactor,
    int NewIntervalDays) : IDomainEvent;
