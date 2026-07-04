using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.DeleteBlock;

public sealed record DeleteBlockCommand(Guid BlockId) : IRequest<Result>, IInvalidatesBlocksCache;
