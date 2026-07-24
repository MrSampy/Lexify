namespace Lexify.Application.Blocks.Common;

/// <summary>The owner's view of a block's share link. <see cref="Token"/> is the whole credential.</summary>
public sealed record BlockShareDto(
    string Token,
    DateTimeOffset CreatedAt,
    int ViewCount,
    int CopyCount);

/// <summary>
/// What a share link shows its recipient. Deliberately narrow: no owner id, and none of the words'
/// SM-2 state — that is the owner's study history, not part of the vocabulary being shared.
/// </summary>
public sealed record SharedBlockDto(
    string Title,
    string? Description,
    short LanguageId,
    int WordCount,
    /// <summary>Null when the owner never set a display name — the UI then just omits the attribution.</summary>
    string? OwnerDisplayName,
    IReadOnlyList<SharedWordDto> Words);

public sealed record SharedWordDto(
    string Term,
    string Translation,
    IReadOnlyList<string> AlternativeTranslations,
    IReadOnlyList<string> Synonyms,
    string WordType,
    string? Notes,
    string? ExampleSentence);
