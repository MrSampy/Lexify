namespace Lexify.API.Requests.Blocks;

public sealed record CreateBlockRequest(
    short LanguageId,
    string Title,
    string? Description);
