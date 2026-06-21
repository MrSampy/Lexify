namespace Lexify.API.Requests.Blocks;

public sealed record UpdateBlockRequest(
    string Title,
    string? Description);
