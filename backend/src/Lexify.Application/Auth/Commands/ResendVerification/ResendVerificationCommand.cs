using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.ResendVerification;

public sealed record ResendVerificationCommand(string Email) : IRequest<Result>;
