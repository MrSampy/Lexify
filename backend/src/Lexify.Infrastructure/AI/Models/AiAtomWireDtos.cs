namespace Lexify.Infrastructure.AI.Models;

/// <summary>Raw shape of the LLM's fill-sentence response — mirrors AiJsonSchemas.FillSentencesResult.</summary>
internal sealed record FillSentencesWireResult(IReadOnlyList<FillSentenceWireItem>? Sentences);

internal sealed record FillSentenceWireItem(string Id, string Sentence);

/// <summary>Raw shape of the LLM's definitions response — mirrors AiJsonSchemas.DefinitionsResult.</summary>
internal sealed record DefinitionsWireResult(IReadOnlyList<DefinitionWireItem>? Definitions);

internal sealed record DefinitionWireItem(string Id, string Definition);

/// <summary>Raw shape of the LLM's fake-distractors response — mirrors AiJsonSchemas.DistractorsResult.</summary>
internal sealed record DistractorsWireResult(IReadOnlyList<string>? Distractors);
