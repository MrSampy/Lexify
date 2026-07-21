namespace Lexify.Application.AI.Dtos;

/// <summary>One LLM-generated monolingual definition, matched back to its word by id.</summary>
public sealed record DefinitionAtom(Guid WordId, string Definition);
