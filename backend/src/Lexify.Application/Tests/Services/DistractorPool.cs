using Lexify.Domain.Entities;

namespace Lexify.Application.Tests.Services;

/// <summary>
/// Supplies plausible-but-wrong quiz options, prioritizing real words the user already has over
/// LLM-invented ones: (1) words from the user's OTHER blocks in the same language — the most
/// plausible distractors, since they're vocabulary the user is also actually learning — then
/// (2) words from the current test's own blocks. GenerateTestJob falls back to
/// IAIProvider.GenerateFakeDistractorsAsync only when this pool can't supply at least 3.
/// </summary>
public sealed class DistractorPool(IReadOnlyList<Word> crossBlockWords, IReadOnlyList<Word> sameBlockWords)
{
    public IReadOnlyList<string> TakeTranslations(Word target, int count, Random rng) =>
        Take(target, count, rng, w => w.Translation,
            [target.Translation, .. target.AlternativeTranslations]);

    public IReadOnlyList<string> TakeTerms(Word target, int count, Random rng) =>
        Take(target, count, rng, w => w.Term, [target.Term]);

    private List<string> Take(
        Word target, int count, Random rng, Func<Word, string> select, IEnumerable<string> exclude)
    {
        var excluded = new HashSet<string>(exclude, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var pool in new[] { crossBlockWords, sameBlockWords })
        {
            if (result.Count >= count) break;

            foreach (var word in pool.Where(w => w.Id != target.Id).OrderBy(_ => rng.Next()))
            {
                if (result.Count >= count) break;

                var value = select(word);
                if (excluded.Contains(value) || !seen.Add(value)) continue;

                result.Add(value);
            }
        }

        return result;
    }
}
