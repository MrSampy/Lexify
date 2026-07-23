using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    ISystemSettingRepository settingRepository,
    IPasswordHasher passwordHasher,
    IEmailVerificationService emailVerification,
    IUnitOfWork unitOfWork,
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

        var verificationRequired = await emailVerification.IsRequiredAsync(cancellationToken);

        // Nothing to prove when confirmation is switched off — the account is usable immediately, as
        // it was before this feature existed.
        if (!verificationRequired)
            user.MarkEmailVerified();

        await userRepository.AddAsync(user, cancellationToken);

        if (verificationRequired)
        {
            // The token's FK points at the user, so the row has to exist first.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Welcome mail waits until the address is confirmed — two emails at once would bury the
            // one the user actually has to act on.
            await emailVerification.IssueAsync(
                user, EmailVerificationToken.Purposes.Signup, ct: cancellationToken);
        }
        else
        {
            backgroundJobService.EnqueueWelcomeEmail(user.Email, DisplayNameFor(request));
        }

        return Result.Ok(user.Id);
    }

    private static string DisplayNameFor(RegisterCommand request) =>
        string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.Email.Split('@')[0]
            : request.DisplayName;

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
