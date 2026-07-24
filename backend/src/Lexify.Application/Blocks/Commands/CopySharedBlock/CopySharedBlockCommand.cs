using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.CopySharedBlock;

/// <summary>Clones a shared block into the caller's account. Returns the new block's id.</summary>
public sealed record CopySharedBlockCommand(string Token) : IRequest<Result<Guid>>;
