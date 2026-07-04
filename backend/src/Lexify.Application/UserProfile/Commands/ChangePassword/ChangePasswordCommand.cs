using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Result>;
