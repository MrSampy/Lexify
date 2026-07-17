using Lexify.Application.Blocks.Dtos;
using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;

namespace Lexify.Application.Blocks.Queries.GetBlockById;

/// <param name="FlaggedCount">Confidence-flagged words in the whole block, not just the current page.</param>
public sealed record BlockDetailDto(
    WordBlockDto Block,
    PagedResult<WordDto> Words,
    int FlaggedCount);
