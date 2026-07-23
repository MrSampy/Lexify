using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.VerifyUserEmail;

/// <summary>Admin override for when a user cannot receive the confirmation email at all.</summary>
public sealed record VerifyUserEmailCommand(Guid UserId) : IRequest<Result>;
