using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.RevokeBlockShare;

/// <summary>Turns sharing off: the link stops working immediately. Copies already made are unaffected.</summary>
public sealed record RevokeBlockShareCommand(Guid BlockId) : IRequest<Result>;
