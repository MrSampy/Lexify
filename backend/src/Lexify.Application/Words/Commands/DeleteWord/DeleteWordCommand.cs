using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Words.Commands.DeleteWord;

public sealed record DeleteWordCommand(Guid WordId) : IRequest<Result>, IInvalidatesBlocksCache;
