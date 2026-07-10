namespace Lexify.Application.AI.Dtos;

/// <summary>
/// One word needing an example sentence generated for it. PreviousError is set only on the
/// one-shot retry, so the LLM sees exactly why its first attempt for this word failed.
/// </summary>
public sealed record FillSentenceRequest(Guid WordId, string Term, string Translation, string? PreviousError = null);
