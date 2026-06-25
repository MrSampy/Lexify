using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.RemoveTagFromBlock;

public sealed record RemoveTagFromBlockCommand(Guid BlockId, string TagName) : IRequest<Result>;
