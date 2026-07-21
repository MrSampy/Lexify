namespace Lexify.Application.Tests.Dtos;

/// <summary>
/// AudioText / WordId / LanguageId are populated ONLY for listen_and_type — the client needs the
/// term to pronounce it via TTS (server neural audio keyed by WordId + LanguageId, browser voice
/// fallback keyed by LanguageId), which unavoidably exposes the answer to devtools for that type.
/// Every other type keeps CorrectAnswer server-side (these stay null).
/// </summary>
public sealed record QuestionDto(
    Guid Id,
    string QuestionType,
    string QuestionText,
    int SortOrder,
    IReadOnlyList<QuestionOptionDto> Options,
    string? AudioText = null,
    Guid? WordId = null,
    int? LanguageId = null);
