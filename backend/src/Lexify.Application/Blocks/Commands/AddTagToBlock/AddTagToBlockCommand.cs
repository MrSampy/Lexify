using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.AddTagToBlock;

public sealed record AddTagToBlockCommand(Guid BlockId, string TagName) : IRequest<Result>;
