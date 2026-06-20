using System.Text.RegularExpressions;
using Lexify.Domain.Common;

namespace Lexify.Domain.ValueObjects;

public sealed class Language : IEquatable<Language>
{
    private static readonly Regex CodePattern = new(@"^[a-z]{2,5}$", RegexOptions.Compiled);

    public string Code { get; }

    private Language(string code) => Code = code;

    public static Language From(string code)
    {
        var normalized = code.ToLowerInvariant().Trim();
        if (!CodePattern.IsMatch(normalized))
            throw new DomainException($"'{code}' is not a valid BCP 47 language code.");
        return new Language(normalized);
    }

    public bool Equals(Language? other) => other is not null && Code == other.Code;
    public override bool Equals(object? obj) => obj is Language other && Equals(other);
    public override int GetHashCode() => Code.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Code;

    public static bool operator ==(Language? left, Language? right) => Equals(left, right);
    public static bool operator !=(Language? left, Language? right) => !Equals(left, right);
}
