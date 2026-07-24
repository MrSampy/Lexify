using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Lexify.Infrastructure.Services;

/// <summary>
/// <c>{base64url(userId)}.{base64url(HMACSHA256(purpose:userId))}</c>, keyed by the JWT secret.
/// </summary>
public sealed class UnsubscribeTokenService(IOptions<JwtSettings> options) : IUnsubscribeTokenService
{
    // Bound into the signed payload so a token minted here can never be replayed against a future
    // feature that signs user ids with the same key.
    private const string Purpose = "unsubscribe";

    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.SecretKey);

    public string Create(Guid userId)
    {
        var id = ToBase64Url(userId.ToByteArray());
        return $"{id}.{ToBase64Url(Sign(userId))}";
    }

    public bool TryValidate(string? token, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var separator = token.IndexOf('.');
        if (separator <= 0 || separator == token.Length - 1) return false;

        byte[] idBytes, signature;
        try
        {
            idBytes = FromBase64Url(token[..separator]);
            signature = FromBase64Url(token[(separator + 1)..]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (idBytes.Length != 16) return false;

        var candidate = new Guid(idBytes);
        // Fixed-time compare: the signature is the only thing standing between a guessed user id and
        // someone else's subscription.
        if (!CryptographicOperations.FixedTimeEquals(signature, Sign(candidate))) return false;

        userId = candidate;
        return true;
    }

    private byte[] Sign(Guid userId) =>
        HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes($"{Purpose}:{userId}"));

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => throw new FormatException("Invalid base64url length.")
        };
        return Convert.FromBase64String(padded);
    }
}
