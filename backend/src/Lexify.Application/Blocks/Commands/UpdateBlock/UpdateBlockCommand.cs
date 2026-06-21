using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.UpdateBlock;

public sealed record UpdateBlockCommand(
    Guid BlockId,
    string Title,
    string? Description
) : IRequest<Result>;
