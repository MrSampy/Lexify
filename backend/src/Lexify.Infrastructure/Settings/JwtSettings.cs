namespace Lexify.Infrastructure.Settings;

public sealed class JwtSettings
{
    public string SecretKey { get; init; } = default!;
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public int AccessTokenExpiryMinutes { get; init; } = 15;

    /// <summary>
    /// Lifetime of the short-lived 2FA challenge token issued between the password step and the code step.
    /// Short by design — it only needs to outlast the user typing in the code from their email.
    /// </summary>
    public int TwoFactorChallengeExpiryMinutes { get; init; } = 10;
}
