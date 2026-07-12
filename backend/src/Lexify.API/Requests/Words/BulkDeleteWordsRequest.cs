namespace Lexify.API.Requests.Words;

public sealed record BulkDeleteWordsRequest(IReadOnlyList<Guid> WordIds);
