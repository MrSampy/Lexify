namespace Lexify.Application.AI.Dtos;

/// <summary>A single word the conversation is trying to get the learner to use.</summary>
public sealed record TargetWord(Guid WordId, string Term, string Translation);

/// <summary>Framing for a "Talk to Lexi" reply: the languages, the learner's level, and the target words.</summary>
public sealed record ChatContext(
    string TargetLanguage,
    string NativeLanguage,
    string? CefrLevel,
    string? Scenario,
    IReadOnlyList<TargetWord> TargetWords);

/// <summary>One prior turn of the conversation passed back to the model as history.</summary>
public sealed record ChatTurn(string Role, string Content);

/// <summary>
/// The model's post-hoc judgement of whether a single target word was used, and used correctly, over the
/// whole conversation. Drives the SM-2 quality mapped in EndConversation.
/// </summary>
public sealed record WordUsageVerdict(Guid WordId, bool Used, bool UsedCorrectly, string? Note);
