using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Application.Tests.Services;
using Lexify.Domain.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

/// <summary>
/// Builds a test's questions almost entirely in code from the user's own words: translate_to_native,
/// translate_to_foreign, multi_select_theme, and open_answer need no LLM call at all. Only
/// fill_in_sentence needs the LLM (one short example sentence per word, cached on Word afterward),
/// and only the real-word distractor pool running dry falls back to LLM-invented distractors.
/// </summary>
public sealed partial class GenerateTestJob(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    ILanguageRepository languageRepository,
    ITestRepository testRepository,
    IQuestionRepository questionRepository,
    IUserRepository userRepository,
    IAIProvider aiProvider,
    IUnitOfWork unitOfWork,
    ILogger<GenerateTestJob> logger)
{
    private const int FillSentenceBatchSize = 5;

    public async Task RunAsync(
        Guid testId,
        Guid userId,
        Guid[] blockIds,
        string[] questionTypes,
        int questionCount,
        CancellationToken cancellationToken = default)
    {
        var test = await testRepository.GetByIdAsync(testId, cancellationToken);
        if (test is null)
        {
            LogTestNotFound(logger, testId);
            return;
        }

        var words = await wordRepository.GetByBlockIdsAsync(blockIds, cancellationToken);
        var blockLanguageIds = await ResolveBlockLanguagesAsync(blockIds, cancellationToken);
        words = words.Where(w => blockLanguageIds.ContainsKey(w.BlockId)).ToList();

        if (words.Count < 5)
        {
            LogInsufficientWords(logger, testId, words.Count);
            await MarkFailedAsync(test, cancellationToken);
            return;
        }

        var usedHashes = await questionRepository.GetUsedContentHashesByUserAsync(userId, cancellationToken);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        var languages = await languageRepository.GetAllAsync(includeInactive: true, cancellationToken);
        var languageNames = languages.ToDictionary(l => l.Id, l => l.Name);
        var languageNameByWordId = words.ToDictionary(
            w => w.Id,
            w => languageNames.GetValueOrDefault(blockLanguageIds[w.BlockId], "the"));

        var poolsByLanguage = await BuildDistractorPoolsAsync(
            userId, blockIds, words, blockLanguageIds, cancellationToken);

        var requestedTypes = questionTypes
            .Select(MapToDomainType)
            .Where(t => t is not null)
            .Cast<string>()
            .Distinct()
            .ToList();
        if (requestedTypes.Count == 0)
            requestedTypes = [Question.QuestionTypes.TranslateToNative];

        var rng = new Random(testId.GetHashCode());
        var shuffledWords = words.OrderBy(_ => rng.Next()).ToList();

        var plan = PlanQuestions(shuffledWords, requestedTypes, questionCount, usedHashes, languageNameByWordId);

        var wordsNeedingSentence = plan
            .Where(p => p.Type == Question.QuestionTypes.FillInSentence)
            .Select(p => p.Word)
            .Distinct()
            .ToList();
        var blankedSentenceByWordId = await ResolveFillSentencesAsync(
            wordsNeedingSentence, languageNameByWordId, user?.EnglishLevel, cancellationToken);

        var assembledQuestions = new List<AssembledQuestion>();
        var seenHashesThisTest = new HashSet<string>();
        var assembledPairs = new HashSet<(Guid WordId, string Type)>();

        foreach (var (word, type) in plan)
        {
            var pool = poolsByLanguage[blockLanguageIds[word.BlockId]];
            AssembledQuestion? assembled = null;

            switch (type)
            {
                case Question.QuestionTypes.TranslateToNative:
                {
                    var distractors = await GetTranslationDistractorsAsync(pool, word, 3, rng, cancellationToken);
                    assembled = TestQuestionAssembler.TranslateToNative(word, distractors);
                    break;
                }
                case Question.QuestionTypes.TranslateToForeign:
                {
                    var distractors = await GetTermDistractorsAsync(pool, word, 3, rng, cancellationToken);
                    assembled = TestQuestionAssembler.TranslateToForeign(word, languageNameByWordId[word.Id], distractors);
                    break;
                }
                case Question.QuestionTypes.MultiSelectTheme:
                {
                    var correctCount = 1 + Math.Min(word.AlternativeTranslations.Count, 2);
                    var neededDistractors = Math.Max(5 - correctCount, 2);
                    var distractors = await GetTranslationDistractorsAsync(pool, word, neededDistractors, rng, cancellationToken);
                    assembled = TestQuestionAssembler.MultiSelectTheme(word, distractors);
                    break;
                }
                case Question.QuestionTypes.OpenAnswer:
                    assembled = TestQuestionAssembler.OpenAnswer(word);
                    break;
                case Question.QuestionTypes.FillInSentence:
                {
                    if (blankedSentenceByWordId.TryGetValue(word.Id, out var sentence))
                    {
                        var distractors = await GetTermDistractorsAsync(pool, word, 3, rng, cancellationToken);
                        assembled = TestQuestionAssembler.FillInSentence(word, sentence, distractors);
                    }
                    else if (!assembledPairs.Contains((word.Id, Question.QuestionTypes.TranslateToNative)))
                    {
                        // Sentence generation failed twice — fall back to a question type that
                        // never needs the LLM rather than dropping this word's slot entirely.
                        var distractors = await GetTranslationDistractorsAsync(pool, word, 3, rng, cancellationToken);
                        assembled = TestQuestionAssembler.TranslateToNative(word, distractors);
                    }
                    break;
                }
            }

            if (assembled is null) continue;
            if (string.IsNullOrWhiteSpace(assembled.QuestionText) ||
                string.IsNullOrWhiteSpace(assembled.CorrectAnswer)) continue;

            // Soft dedup: templated question text is identical across every regeneration of the same
            // (word, type), so strictly honoring "already used by this user" (as the old heuristic
            // did) would empty every repeat test. usedHashes is only consulted during planning to
            // PREFER fresh combos; here we just guard against two IDENTICAL questions in one test.
            var contentHash = Question.ComputeContentHash(assembled.QuestionType, assembled.QuestionText);
            if (!seenHashesThisTest.Add(contentHash)) continue;

            assembledPairs.Add((word.Id, assembled.QuestionType));
            assembledQuestions.Add(assembled);
        }

        // Top-up: assembly can deliver fewer questions than requested — e.g. a failed fill-sentence
        // falls back to translate_to_native, which then collides (same content hash) with an
        // already-planned (word, translate_to_native) pair, silently eating a slot. Refill from
        // unused LLM-free (word, type) pairs so the user gets exactly the count they asked for
        // whenever enough unique pairs exist.
        if (assembledQuestions.Count < questionCount)
        {
            var topUpTypes = requestedTypes
                .Where(t => t != Question.QuestionTypes.FillInSentence)
                .Concat([
                    Question.QuestionTypes.TranslateToNative,
                    Question.QuestionTypes.TranslateToForeign,
                    Question.QuestionTypes.OpenAnswer])
                .Distinct()
                .ToList();

            foreach (var type in topUpTypes)
            {
                foreach (var word in shuffledWords)
                {
                    if (assembledQuestions.Count >= questionCount) break;
                    if (assembledPairs.Contains((word.Id, type))) continue;
                    if (type == Question.QuestionTypes.MultiSelectTheme && word.AlternativeTranslations.Count == 0)
                        continue;

                    var pool = poolsByLanguage[blockLanguageIds[word.BlockId]];
                    AssembledQuestion topUp = type switch
                    {
                        Question.QuestionTypes.TranslateToForeign => TestQuestionAssembler.TranslateToForeign(
                            word, languageNameByWordId[word.Id],
                            await GetTermDistractorsAsync(pool, word, 3, rng, cancellationToken)),
                        Question.QuestionTypes.MultiSelectTheme => TestQuestionAssembler.MultiSelectTheme(
                            word, await GetTranslationDistractorsAsync(pool, word, 3, rng, cancellationToken)),
                        Question.QuestionTypes.OpenAnswer => TestQuestionAssembler.OpenAnswer(word),
                        _ => TestQuestionAssembler.TranslateToNative(
                            word, await GetTranslationDistractorsAsync(pool, word, 3, rng, cancellationToken)),
                    };

                    var hash = Question.ComputeContentHash(topUp.QuestionType, topUp.QuestionText);
                    if (!seenHashesThisTest.Add(hash)) continue;

                    assembledPairs.Add((word.Id, topUp.QuestionType));
                    assembledQuestions.Add(topUp);
                }

                if (assembledQuestions.Count >= questionCount) break;
            }
        }

        // The plan interleaves words in a predictable word1,word2,... order — shuffle so the final
        // test doesn't present questions in that pattern (or group repeats of a word back-to-back).
        var questions = new List<Question>();
        var options = new List<QuestionOption>();
        var sortOrder = 0;

        foreach (var assembled in assembledQuestions.OrderBy(_ => rng.Next()))
        {
            Question question;
            try
            {
                question = Question.Create(
                    testId, assembled.WordId, assembled.QuestionType,
                    assembled.QuestionText, assembled.CorrectAnswer, sortOrder);
            }
            catch (DomainException)
            {
                continue;
            }

            questions.Add(question);
            sortOrder++;

            var optSort = 0;
            foreach (var opt in assembled.Options)
                options.Add(new QuestionOption(question.Id, opt.Text, opt.IsCorrect, optSort++));
        }

        if (questions.Count == 0)
        {
            LogNoQuestionsGenerated(logger, testId);
            await MarkFailedAsync(test, cancellationToken);
            return;
        }

        await questionRepository.AddRangeAsync(questions, cancellationToken);
        await questionRepository.AddOptionsRangeAsync(options, cancellationToken);

        test.MarkReady(questions.Count);
        await testRepository.UpdateAsync(test, cancellationToken);

        // Also persists any Word.SetExampleSentence write-backs from ResolveFillSentencesAsync —
        // same DbContext, same SaveChanges call.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogTestReady(logger, testId, questions.Count);
    }

    private async Task<Dictionary<Guid, short>> ResolveBlockLanguagesAsync(
        Guid[] blockIds, CancellationToken ct)
    {
        var result = new Dictionary<Guid, short>();
        foreach (var blockId in blockIds)
        {
            var block = await blockRepository.GetByIdAsync(blockId, ct);
            if (block is not null)
                result[blockId] = block.LanguageId;
        }
        return result;
    }

    private async Task<Dictionary<short, DistractorPool>> BuildDistractorPoolsAsync(
        Guid userId, Guid[] blockIds, IReadOnlyList<Word> words,
        Dictionary<Guid, short> blockLanguageIds, CancellationToken ct)
    {
        var testBlockIds = blockIds.ToHashSet();
        var pools = new Dictionary<short, DistractorPool>();

        foreach (var languageId in blockLanguageIds.Values.Distinct())
        {
            var crossBlock = await wordRepository.GetDistractorPoolAsync(userId, languageId, 60, ct);
            var crossBlockOutsideTest = crossBlock.Where(w => !testBlockIds.Contains(w.BlockId)).ToList();
            var sameBlock = words.Where(w => blockLanguageIds.GetValueOrDefault(w.BlockId) == languageId).ToList();
            pools[languageId] = new DistractorPool(crossBlockOutsideTest, sameBlock);
        }

        return pools;
    }

    /// <summary>
    /// Distributes questionCount across the requested types and shuffled words, in two passes: the
    /// first prefers (word, type) combos whose content hash this user hasn't seen in a prior test,
    /// the second allows reuse to fill any remaining slots. fill_in_sentence combos skip the hash
    /// check entirely — their eventual text depends on a not-yet-generated sentence.
    ///
    /// Candidate order iterates WORDS fastest (word changes every step, type rotates per round with
    /// a per-word stagger) so every word gets one question before any word gets a second — a test
    /// never clusters most of its questions on one word just because multiple types were requested.
    /// multi_select_theme ("select ALL correct translations") is only meaningful with 2+ correct
    /// answers, so words without alternative translations get the next requested type instead.
    /// </summary>
    private static List<(Word Word, string Type)> PlanQuestions(
        List<Word> shuffledWords,
        List<string> requestedTypes,
        int questionCount,
        IReadOnlySet<string> usedHashes,
        Dictionary<Guid, string> languageNameByWordId)
    {
        var maxCandidates = Math.Max(questionCount * 6, requestedTypes.Count * shuffledWords.Count);
        var candidates = new List<(Word Word, string Type)>(maxCandidates);
        for (var i = 0; candidates.Count < maxCandidates; i++)
        {
            var wordIndex = i % shuffledWords.Count;
            var round = i / shuffledWords.Count;
            var word = shuffledWords[wordIndex];
            var type = PickEligibleType(word, requestedTypes, round + wordIndex);
            candidates.Add((word, type));
        }

        string? HashOf(Word w, string type) => type switch
        {
            Question.QuestionTypes.TranslateToNative =>
                Question.ComputeContentHash(type, QuestionTemplates.TranslateToNative(w.Term)),
            Question.QuestionTypes.TranslateToForeign =>
                Question.ComputeContentHash(type,
                    QuestionTemplates.TranslateToForeign(w.Translation, languageNameByWordId.GetValueOrDefault(w.Id, "the"))),
            Question.QuestionTypes.MultiSelectTheme =>
                Question.ComputeContentHash(type, QuestionTemplates.MultiSelectTheme(w.Term)),
            Question.QuestionTypes.OpenAnswer =>
                Question.ComputeContentHash(type, QuestionTemplates.OpenAnswer(w.Term)),
            _ => null // fill_in_sentence: text depends on a sentence that doesn't exist yet
        };

        var chosen = new List<(Word Word, string Type)>();
        var chosenPairs = new HashSet<(Guid, string)>();
        var chosenHashesInTest = new HashSet<string>();

        void TrySelect(bool allowUsedHash)
        {
            foreach (var candidate in candidates)
            {
                if (chosen.Count >= questionCount) return;
                if (chosenPairs.Contains((candidate.Word.Id, candidate.Type))) continue;

                var hash = HashOf(candidate.Word, candidate.Type);
                if (hash is not null)
                {
                    if (!allowUsedHash && usedHashes.Contains(hash)) continue;
                    if (!chosenHashesInTest.Add(hash)) continue;
                }

                chosenPairs.Add((candidate.Word.Id, candidate.Type));
                chosen.Add(candidate);
            }
        }

        TrySelect(allowUsedHash: false);
        if (chosen.Count < questionCount)
            TrySelect(allowUsedHash: true);

        return chosen;
    }

    /// <summary>
    /// Rotates through the requested types starting at <paramref name="startOffset"/>, skipping
    /// multi_select_theme for words that can't support it (fewer than 2 correct answers). When no
    /// other requested type exists, falls back to translate_to_native — an always-valid, LLM-free type.
    /// </summary>
    private static string PickEligibleType(Word word, List<string> requestedTypes, int startOffset)
    {
        for (var offset = 0; offset < requestedTypes.Count; offset++)
        {
            var type = requestedTypes[(startOffset + offset) % requestedTypes.Count];
            if (type == Question.QuestionTypes.MultiSelectTheme && word.AlternativeTranslations.Count == 0)
                continue;
            return type;
        }

        return Question.QuestionTypes.TranslateToNative;
    }

    /// <summary>
    /// Resolves a ready-to-use (already blanked) sentence for every given word: reuses a cached
    /// ExampleSentence when it contains the term, otherwise generates one via the LLM (batched,
    /// grouped by language) and caches the result back onto the word for future tests. A word whose
    /// generation fails twice is simply absent from the returned dictionary.
    /// </summary>
    private async Task<Dictionary<Guid, string>> ResolveFillSentencesAsync(
        IReadOnlyList<Word> wordsNeedingSentence,
        Dictionary<Guid, string> languageNameByWordId,
        string? englishLevel,
        CancellationToken ct)
    {
        var blankedByWordId = new Dictionary<Guid, string>();
        var toGenerate = new List<Word>();

        foreach (var word in wordsNeedingSentence)
        {
            if (FillSentenceValidator.Check(word.ExampleSentence, word.Term) is { IsValid: true })
                blankedByWordId[word.Id] = FillSentenceValidator.Blank(word.ExampleSentence!, word.Term);
            else
                toGenerate.Add(word);
        }

        foreach (var languageGroup in toGenerate.GroupBy(w => languageNameByWordId.GetValueOrDefault(w.Id, "the")))
        {
            foreach (var batch in languageGroup.Chunk(FillSentenceBatchSize))
            {
                var wordById = batch.ToDictionary(w => w.Id);
                var resolved = await GenerateAndValidateSentencesAsync(
                    batch, languageGroup.Key, englishLevel, wordById, ct);

                foreach (var (word, sentence) in resolved)
                {
                    if (string.IsNullOrEmpty(word.ExampleSentence))
                        word.SetExampleSentence(sentence);
                    blankedByWordId[word.Id] = FillSentenceValidator.Blank(sentence, word.Term);
                }
            }
        }

        return blankedByWordId;
    }

    private async Task<List<(Word Word, string Sentence)>> GenerateAndValidateSentencesAsync(
        IReadOnlyList<Word> batch,
        string targetLanguage,
        string? englishLevel,
        Dictionary<Guid, Word> wordById,
        CancellationToken ct)
    {
        var valid = new List<(Word, string)>();

        var requests = batch.Select(w => new FillSentenceRequest(w.Id, w.Term, w.Translation)).ToList();
        var atoms = await aiProvider.GenerateFillSentencesAsync(requests, targetLanguage, englishLevel, ct);
        var atomByWordId = atoms.ToDictionary(a => a.WordId);

        var failed = new Dictionary<Guid, string>();
        foreach (var req in requests)
        {
            if (!atomByWordId.TryGetValue(req.WordId, out var atom))
            {
                failed[req.WordId] = "No sentence was returned for this word.";
                continue;
            }

            var check = FillSentenceValidator.Check(atom.Sentence, req.Term);
            if (check.IsValid)
                valid.Add((wordById[req.WordId], check.Sentence!));
            else
                failed[req.WordId] = check.ErrorMessage!;
        }

        if (failed.Count == 0) return valid;

        var retryRequests = failed
            .Select(kv => wordById[kv.Key])
            .Select(w => new FillSentenceRequest(w.Id, w.Term, w.Translation, failed[w.Id]))
            .ToList();

        var retryAtoms = await aiProvider.GenerateFillSentencesAsync(retryRequests, targetLanguage, englishLevel, ct);
        var retryByWordId = retryAtoms.ToDictionary(a => a.WordId);

        foreach (var req in retryRequests)
        {
            if (!retryByWordId.TryGetValue(req.WordId, out var atom)) continue;

            var check = FillSentenceValidator.Check(atom.Sentence, req.Term);
            if (check.IsValid)
                valid.Add((wordById[req.WordId], check.Sentence!));
            // Second failure: word gets no sentence — the caller falls back to another question type.
        }

        return valid;
    }

    private async Task<IReadOnlyList<string>> GetTranslationDistractorsAsync(
        DistractorPool pool, Word target, int count, Random rng, CancellationToken ct)
    {
        var real = pool.TakeTranslations(target, count, rng);
        return real.Count >= 3 ? real : await SupplementWithFakeAsync(real, target.Translation, count, ct);
    }

    private async Task<IReadOnlyList<string>> GetTermDistractorsAsync(
        DistractorPool pool, Word target, int count, Random rng, CancellationToken ct)
    {
        var real = pool.TakeTerms(target, count, rng);
        return real.Count >= 3 ? real : await SupplementWithFakeAsync(real, target.Term, count, ct);
    }

    private async Task<IReadOnlyList<string>> SupplementWithFakeAsync(
        IReadOnlyList<string> real, string correctAnswer, int count, CancellationToken ct)
    {
        var missing = count - real.Count;
        if (missing <= 0) return real;

        var fake = await aiProvider.GenerateFakeDistractorsAsync(correctAnswer, missing, ct);
        var combined = new List<string>(real);
        foreach (var candidate in fake)
        {
            if (combined.Count >= count) break;
            if (!string.Equals(candidate, correctAnswer, StringComparison.OrdinalIgnoreCase) &&
                !combined.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                combined.Add(candidate);
        }
        return combined;
    }

    /// <summary>
    /// Moves the test to 'failed' so the frontend poller stops instead of waiting forever
    /// on a test that will never become ready.
    /// </summary>
    private async Task MarkFailedAsync(Test test, CancellationToken cancellationToken)
    {
        test.MarkFailed();
        await testRepository.UpdateAsync(test, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? MapToDomainType(string type) =>
        Question.QuestionTypes.All.Contains(type) ? type : null;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Test {TestId} not found for generation")]
    private static partial void LogTestNotFound(ILogger logger, Guid testId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Test {TestId} has insufficient words ({Count} < 5)")]
    private static partial void LogInsufficientWords(ILogger logger, Guid testId, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No valid questions generated for test {TestId}")]
    private static partial void LogNoQuestionsGenerated(ILogger logger, Guid testId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Test {TestId} is ready with {Count} questions")]
    private static partial void LogTestReady(ILogger logger, Guid testId, int count);
}
