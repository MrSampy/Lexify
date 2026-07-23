using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;

namespace Lexify.Application.Auth.Common;

/// <summary>
/// Owns the two-factor (email code) rules: whether a given sign-in needs a second factor, issuing the
/// one-time code, and verifying it. The policy (global switch + admin mandate + per-user opt-in) lives
/// here rather than in the handlers because login, verify, resend and the profile-enable flow all share it.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>The master switch (<see cref="SystemSetting.Keys.TwoFactorEnabled"/>).</summary>
    Task<bool> IsGloballyEnabledAsync(CancellationToken ct = default);

    /// <summary>Whether this user must pass a second factor at sign-in right now.</summary>
    Task<bool> IsRequiredForAsync(User user, CancellationToken ct = default);

    /// <summary>Supersedes any outstanding code, mints a fresh one, stores its hash, emails it.</summary>
    Task IssueCodeAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// True when <paramref name="code"/> matches the user's active code (which is then consumed). A wrong
    /// code is counted toward the lockout and returns false.
    /// </summary>
    Task<bool> VerifyCodeAsync(User user, string code, CancellationToken ct = default);

    static string HashCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}

public sealed class TwoFactorService(
    ILoginTwoFactorCodeRepository codeRepository,
    ISystemSettingRepository settingRepository,
    IBackgroundJobService backgroundJobService)
    : ITwoFactorService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    public async Task<bool> IsGloballyEnabledAsync(CancellationToken ct = default)
    {
        var setting = await settingRepository.GetByKeyAsync(
            SystemSetting.Keys.TwoFactorEnabled, ct);

        // Fail OPEN, unlike email verification: a missing/unparseable row must not lock admins out of an
        // app whose mail path may itself be the thing that broke — so an absent row reads as OFF. The row
        // ships DORMANT (seeded "false", see DataSeeder): turning it on forces every admin through an
        // emailed code on their next sign-in, so activation is a deliberate operator step, not sprung by a
        // deploy. Once on, flipping it back to "false" is the kill switch.
        return setting is not null && bool.TryParse(setting.Value, out var enabled) && enabled;
    }

    public async Task<bool> IsRequiredForAsync(User user, CancellationToken ct = default)
    {
        if (!await IsGloballyEnabledAsync(ct)) return false;
        if (user.IsTwoFactorMandatory) return true; // admins are forced regardless of the opt-in flag
        return user.TwoFactorEnabled;
    }

    public async Task IssueCodeAsync(User user, CancellationToken ct = default)
    {
        // A fresh code supersedes any previously issued one.
        await codeRepository.InvalidateActiveForUserAsync(user.Id, ct);

        // Uniform 6-digit code, zero-padded; RandomNumberGenerator avoids the modulo bias of Random.
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

        var entity = new LoginTwoFactorCode(
            user.Id,
            ITwoFactorService.HashCode(code),
            DateTimeOffset.UtcNow.Add(CodeLifetime));

        await codeRepository.AddAsync(entity, ct);
        backgroundJobService.EnqueueTwoFactorCode(user.Email, code);
    }

    public async Task<bool> VerifyCodeAsync(User user, string code, CancellationToken ct = default)
    {
        var active = await codeRepository.GetActiveForUserAsync(user.Id, ct);
        if (active is null) return false;

        if (ITwoFactorService.HashCode(code) == active.CodeHash)
        {
            // Success only if THIS call atomically claimed the code (1 row). If a concurrent verify
            // already consumed it (0 rows), treat as failure so one code can never yield two sessions.
            var claimed = await codeRepository.MarkUsedAsync(active.Id, ct);
            return claimed == 1;
        }

        // Persisted immediately (not a tracked mutation): a failed verify returns a failing Result and the
        // transaction behavior skips SaveChanges, so a tracked Attempts++ would never stick — see the repo.
        await codeRepository.IncrementAttemptsAsync(active.Id, ct);
        return false;
    }
}
