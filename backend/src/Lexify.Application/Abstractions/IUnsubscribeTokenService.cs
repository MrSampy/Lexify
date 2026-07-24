namespace Lexify.Application.Abstractions;

/// <summary>
/// Mints and checks the token embedded in the unsubscribe link of reminder emails. Stateless (a keyed
/// signature, not a stored row) because the link must keep working for as long as the mail exists in
/// someone's inbox — a table of these would either grow without bound or expire live links.
/// </summary>
public interface IUnsubscribeTokenService
{
    /// <summary>Signs an unsubscribe link for <paramref name="userId"/>. Stable across calls.</summary>
    string Create(Guid userId);

    /// <summary>
    /// Checks the signature and recovers the user id. False for anything tampered with, truncated, or
    /// minted for a different purpose.
    /// </summary>
    bool TryValidate(string? token, out Guid userId);
}
