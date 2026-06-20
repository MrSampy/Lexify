using Lexify.Domain.Common;
using Lexify.Domain.ValueObjects;

namespace Lexify.Domain.Events;

public sealed record TestCompletedEvent(
    Guid AttemptId,
    Guid TestId,
    Guid UserId,
    TestScore Score) : IDomainEvent;
