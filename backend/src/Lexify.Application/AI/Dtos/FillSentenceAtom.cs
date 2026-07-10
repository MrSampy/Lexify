namespace Lexify.Application.AI.Dtos;

/// <summary>One LLM-generated example sentence, matched back to its word by id.</summary>
public sealed record FillSentenceAtom(Guid WordId, string Sentence);
