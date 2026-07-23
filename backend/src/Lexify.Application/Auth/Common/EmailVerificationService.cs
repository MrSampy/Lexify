using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;

namespace Lexify.Application.Auth.Common;

/// <summary>
/// Issues the single-use confirmation links. Three flows need the exact same sequence — supersede the
/// user's outstanding links, mint a token, store only its hash, queue the email — so it lives in one
/// place rather than being re-derived in the register, resend and email-change handlers.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>Whether unconfirmed accounts are blocked from signing in (admin-configurable).</summary>
    Task<bool> IsRequiredAsync(CancellationToken ct = default);

    /// <param name="newEmail">Only for <see cref="EmailVerificationToken.Purposes.EmailChange"/>; the link is sent there.</param>
    Task IssueAsync(User user, string purpose, string? newEmail = null, CancellationToken ct = default);

    /// <summary>Hashes a raw token from a link the same way <see cref="IssueAsync"/> stored it.</summary>
    static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}

public sealed class EmailVerificationService(
    IEmailVerificationTokenRepository tokenRepository,
    ISystemSettingRepository settingRepository,
    IBackgroundJobService backgroundJobService)
    : IEmailVerificationService
{
    /// <summary>
    /// A day, not the password-reset hour: sign-up mail is often read the next morning, and an expired
    /// link on first contact with the product is a bad first impression.
    /// </summary>
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public async Task<bool> IsRequiredAsync(CancellationToken ct = default)
    {
        var setting = await settingRepository.GetByKeyAsync(
            SystemSetting.Keys.EmailVerificationRequired, ct);

        // Default to required: a missing row must fail closed, not silently let anyone in.
        return setting is null || !bool.TryParse(setting.Value, out var required) || required;
    }

    public async Task IssueAsync(
        User user, string purpose, string? newEmail = null, CancellationToken ct = default)
    {
        // A fresh request supersedes any previously issued link of the same kind.
        await tokenRepository.InvalidateActiveForUserAsync(user.Id, purpose, ct);

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var token = new EmailVerificationToken(
            user.Id,
            IEmailVerificationService.HashToken(rawToken),
            purpose,
            DateTimeOffset.UtcNow.Add(TokenLifetime),
            newEmail);

        await tokenRepository.AddAsync(token, ct);

        // For an address change the link must go to the *new* inbox — that is what is being proven.
        var recipient = newEmail ?? user.Email;
        backgroundJobService.EnqueueEmailVerification(recipient, rawToken, purpose);
    }
}
