using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    ISystemSettingRepository settingRepository,
    IPasswordHasher passwordHasher,
    IBackgroundJobService backgroundJobService)
    : IRequestHandler<RegisterCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var gate = await CheckRegistrationAllowedAsync(request.InviteCode, cancellationToken);
        if (!gate.IsSuccess)
            return Result.Failure<Guid>(gate.Error!);

        var existing = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result.Failure<Guid>("A user with this email already exists.");

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash, request.DisplayName);

        await userRepository.AddAsync(user, cancellationToken);

        var username = string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.Email.Split('@')[0]
            : request.DisplayName;
        backgroundJobService.EnqueueWelcomeEmail(user.Email, username);

        return Result.Ok(user.Id);
    }

    /// <summary>
    /// Open registration lets anyone in. Once it is closed, the only way in is the shared invite code
    /// from settings — and if no code has been set, registration is closed outright rather than open
    /// to anyone who sends a blank code.
    /// </summary>
    private async Task<(bool IsSuccess, string? Error)> CheckRegistrationAllowedAsync(
        string? providedCode, CancellationToken ct)
    {
        var enabled = await settingRepository.GetByKeyAsync(
            SystemSetting.Keys.RegistrationEnabled, ct);

        // Default to open: a missing row must not lock everyone out of a fresh install.
        if (enabled is null || bool.TryParse(enabled.Value, out var isOpen) && isOpen)
            return (true, null);

        var inviteCode = await settingRepository.GetByKeyAsync(SystemSetting.Keys.InviteCode, ct);
        if (inviteCode is null || string.IsNullOrWhiteSpace(inviteCode.Value))
            return (false, "Registration is currently closed.");

        if (string.IsNullOrWhiteSpace(providedCode) || !CodesMatch(providedCode, inviteCode.Value))
            return (false, "Registration is invite-only. The invite code is missing or incorrect.");

        return (true, null);
    }

    /// <summary>
    /// Constant-time comparison so the code can't be recovered one character at a time by timing the
    /// response. Differing lengths short-circuit — that leaks only the length, not the content.
    /// </summary>
    private static bool CodesMatch(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided.Trim());
        var expectedBytes = Encoding.UTF8.GetBytes(expected.Trim());

        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
