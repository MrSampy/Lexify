using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.CreateBlock;

public sealed record CreateBlockCommand(
    short LanguageId,
    string Title,
    string? Description
) : IRequest<Result<Guid>>, IInvalidatesBlocksCache;
