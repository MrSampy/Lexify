namespace Lexify.Application.Conversations.Dtos;

/// <summary>A word the conversation is trying to get the learner to use, for display in the client.</summary>
public sealed record TargetWordDto(Guid WordId, string Term, string Translation);

public sealed record ConversationMessageDto(Guid Id, string Role, string Content, DateTimeOffset CreatedAt);

/// <summary>List-view row: no messages, just enough to render a history entry.</summary>
public sealed record ConversationListItemDto(
    Guid Id,
    short LanguageId,
    string Title,
    string? Scenario,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EndedAt,
    int MessageCount);

/// <summary>Full transcript for resuming/reviewing a conversation.</summary>
public sealed record ConversationDetailDto(
    Guid Id,
    short LanguageId,
    string Title,
    string? Scenario,
    string Status,
    IReadOnlyList<TargetWordDto> TargetWords,
    IReadOnlyList<ConversationMessageDto> Messages);

/// <summary>Everything the client needs to open a live chat right after starting it.</summary>
public sealed record StartConversationResultDto(
    Guid ConversationId,
    short LanguageId,
    string TargetLanguage,
    string Title,
    string? Scenario,
    IReadOnlyList<TargetWordDto> TargetWords,
    string OpeningMessage,
    int MessageBudget);

/// <summary>
/// Per-target-word outcome shown on the end screen: whether it was used/used-correctly, and — when that
/// fed SM-2 — the resulting schedule so the UI can say "next review in N days".
/// </summary>
public sealed record WordUsageResultDto(
    Guid WordId,
    string Term,
    string Translation,
    bool Used,
    bool UsedCorrectly,
    string? Note,
    int? IntervalDays,
    DateTimeOffset? NextReviewAt);

/// <summary>Challenge outcome for the end screen (see ConversationScoring).</summary>
public sealed record ConversationScoreDto(
    int Points,
    int Stars,
    int WordsUsed,
    int TotalWords,
    int MessagesUsed,
    int MessageBudget);

public sealed record ConversationSummaryDto(
    IReadOnlyList<WordUsageResultDto> Words,
    ConversationScoreDto Score);
