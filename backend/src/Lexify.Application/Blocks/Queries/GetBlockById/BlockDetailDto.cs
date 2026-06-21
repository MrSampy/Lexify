using Lexify.Application.Blocks.Dtos;
using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;

namespace Lexify.Application.Blocks.Queries.GetBlockById;

public sealed record BlockDetailDto(
    WordBlockDto Block,
    PagedResult<WordDto> Words);
