namespace Lexify.Application.Tests.Common;

public static class LevenshteinDistance
{
    public static int Calculate(string s, string t)
    {
        if (s.Length == 0) return t.Length;
        if (t.Length == 0) return s.Length;

        var d = new int[s.Length + 1, t.Length + 1];
        for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= t.Length; j++) d[0, j] = j;

        for (int j = 1; j <= t.Length; j++)
            for (int i = 1; i <= s.Length; i++)
                d[i, j] = s[i - 1] == t[j - 1]
                    ? d[i - 1, j - 1]
                    : 1 + Math.Min(d[i - 1, j], Math.Min(d[i, j - 1], d[i - 1, j - 1]));

        return d[s.Length, t.Length];
    }
}
