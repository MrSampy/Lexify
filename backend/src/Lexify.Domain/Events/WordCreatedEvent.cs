using Lexify.Domain.Common;

namespace Lexify.Domain.Events;

public sealed record WordCreatedEvent(Guid WordId, Guid BlockId) : IDomainEvent;
