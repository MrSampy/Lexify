using FluentValidation;
using Lexify.Domain.Entities;

namespace Lexify.Application.Admin.Commands.ChangeUserRole;

public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    private static readonly string[] ValidRoles =
        [User.Roles.User, User.Roles.Moderator, User.Roles.Admin];

    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => ValidRoles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", ValidRoles)}.");
    }
}
