namespace Lexify.API.Requests.Words;

public sealed record BulkMoveWordsRequest(Guid TargetBlockId, IReadOnlyList<Guid> WordIds);
