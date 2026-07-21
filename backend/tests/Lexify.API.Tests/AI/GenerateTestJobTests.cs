using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Lexify.Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lexify.API.Tests.AI;

public class GenerateTestJobTests
{
    private readonly IWordRepository _wordRepository = Substitute.For<IWordRepository>();
    private readonly IWordBlockRepository _blockRepository = Substitute.For<IWordBlockRepository>();
    private readonly ILanguageRepository _languageRepository = Substitute.For<ILanguageRepository>();
    private readonly ITestRepository _testRepository = Substitute.For<ITestRepository>();
    private readonly IQuestionRepository _questionRepository = Substitute.For<IQuestionRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAIProvider _aiProvider = Substitute.For<IAIProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GenerateTestJob _job;

    public GenerateTestJobTests()
    {
        _job = new GenerateTestJob(
            _wordRepository, _blockRepository, _languageRepository, _testRepository,
            _questionRepository, _userRepository, _aiProvider, _unitOfWork,
            Substitute.For<ILogger<GenerateTestJob>>());

        _languageRepository.GetAllAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Language>>([]));
        _wordRepository.GetDistractorPoolAsync(Arg.Any<Guid>(), Arg.Any<short>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Word>>([]));
        _questionRepository.GetUsedContentHashesByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlySet<string>>(new HashSet<string>()));
    }

    private (WordBlock Block, List<Word> Words) SeedBlockWithWords(Guid userId, int count)
    {
        var block = new WordBlock(userId, 1, "Test Block");
        _blockRepository.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);

        var words = Enumerable.Range(1, count)
            .Select(i => new Word(block.Id, $"term{i}", $"переклад{i}"))
            .ToList();
        _wordRepository.GetByBlockIdsAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(block.Id)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Word>>(words));

        return (block, words);
    }

    [Fact]
    public async Task RunAsync_FewerThan5Words_MarksTestFailed()
    {
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 3);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["translate_to_native"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Failed, test.Status);
    }

    [Fact]
    public async Task RunAsync_LlmFreeQuestionType_SucceedsWithoutAnyAiCall()
    {
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 6);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["translate_to_native"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.True(test.QuestionCount > 0);
        await _aiProvider.DidNotReceive().GenerateFillSentencesAsync(
            Arg.Any<IReadOnlyList<FillSentenceRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _aiProvider.DidNotReceive().GenerateFakeDistractorsAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_EveryCombinationAlreadyUsedByThisUser_StillSucceedsViaSoftDedup()
    {
        // Simulates regenerating a test over the same block: since templated question text is
        // identical across every generation of the same (word, type), a strict "never reuse a
        // used hash" rule would empty this test entirely. It must still succeed.
        var userId = Guid.NewGuid();
        var (block, words) = SeedBlockWithWords(userId, 6);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);

        var usedHashes = words
            .Select(w => Question.ComputeContentHash(
                Question.QuestionTypes.TranslateToNative,
                $"What is the translation of '{w.Term}'?"))
            .ToHashSet();
        _questionRepository.GetUsedContentHashesByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlySet<string>>(usedHashes));

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["translate_to_native"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.True(test.QuestionCount > 0);
    }

    [Fact]
    public async Task RunAsync_FillInSentenceWithNoCachedSentence_GeneratesAndCachesSentenceOnWord()
    {
        var userId = Guid.NewGuid();
        var (block, words) = SeedBlockWithWords(userId, 5);

        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);

        _aiProvider.GenerateFillSentencesAsync(
                Arg.Any<IReadOnlyList<FillSentenceRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var requests = callInfo.Arg<IReadOnlyList<FillSentenceRequest>>();
                IReadOnlyList<FillSentenceAtom> atoms = requests
                    .Select(r => new FillSentenceAtom(
                        r.WordId, $"The word {r.Term} appears in this generated example sentence."))
                    .ToList();
                return Task.FromResult(atoms);
            });

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["fill_in_sentence"], 1, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.Contains(words, w => !string.IsNullOrEmpty(w.ExampleSentence));
    }

    [Fact]
    public async Task RunAsync_AllQuestionTypesInvalid_FallsBackToDefaultType()
    {
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 6);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["not_a_real_type"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
    }

    private List<Question> CaptureSavedQuestions()
    {
        var saved = new List<Question>();
        _questionRepository
            .AddRangeAsync(Arg.Do<IEnumerable<Question>>(qs => saved.AddRange(qs)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return saved;
    }

    [Fact]
    public async Task RunAsync_QuestionCountEqualsWordCount_EveryWordGetsExactlyOneQuestion()
    {
        var userId = Guid.NewGuid();
        var (block, words) = SeedBlockWithWords(userId, 6);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["translate_to_native"], 6, CancellationToken.None);

        Assert.Equal(6, saved.Count);
        var perWord = saved.GroupBy(q => q.WordId).ToList();
        Assert.Equal(words.Count, perWord.Count);
        Assert.All(perWord, g => Assert.Single(g));
    }

    [Fact]
    public async Task RunAsync_MoreQuestionsThanWords_NoWordIsOverloaded()
    {
        // 5 words, 8 questions, several types: the old plan gave one word every requested type
        // before moving to the next word. Now every word must get a question before any gets a
        // second — so no word can end up with more than ceil(8/5) = 2.
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 5);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(
            Guid.NewGuid(), userId, [block.Id],
            ["translate_to_native", "translate_to_foreign", "open_answer"], 8, CancellationToken.None);

        Assert.Equal(8, saved.Count);
        var maxPerWord = saved.GroupBy(q => q.WordId).Max(g => g.Count());
        Assert.True(maxPerWord <= 2, $"One word received {maxPerWord} of 8 questions across 5 words.");
    }

    [Fact]
    public async Task RunAsync_MultiSelectRequested_WordsWithoutAlternativesGetAnotherType()
    {
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 6); // no alternative translations
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["multi_select_theme"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.NotEmpty(saved);
        // "Select ALL correct translations" is meaningless with a single translation — every slot
        // must have been substituted with the fallback type instead.
        Assert.DoesNotContain(saved, q => q.QuestionType == Question.QuestionTypes.MultiSelectTheme);
    }

    [Fact]
    public async Task RunAsync_MultiSelectRequested_WordsWithAlternativesDoGetIt()
    {
        var userId = Guid.NewGuid();
        var (block, words) = SeedBlockWithWords(userId, 6);
        foreach (var word in words)
            word.SetAlternativeTranslations([$"альт-{word.Term}"]);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["multi_select_theme"], 5, CancellationToken.None);

        Assert.NotEmpty(saved);
        Assert.All(saved, q => Assert.Equal(Question.QuestionTypes.MultiSelectTheme, q.QuestionType));
    }

    [Fact]
    public async Task RunAsync_FillSentencesAllFail_StillDeliversExactRequestedCount()
    {
        // Regression: 5 words + 10 questions of all 5 types delivered 9. When a fill sentence fails
        // twice, its slot falls back to translate_to_native — which collides (same content hash)
        // with the already-planned (word, translate_to_native) pair and used to vanish silently.
        // The top-up pass must refill the slot from another unused pair.
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 5);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        // LLM returns nothing — every fill_in_sentence slot must be refilled by other types
        _aiProvider.GenerateFillSentencesAsync(
                Arg.Any<IReadOnlyList<FillSentenceRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FillSentenceAtom>>([]));

        await _job.RunAsync(
            Guid.NewGuid(), userId, [block.Id],
            ["translate_to_native", "translate_to_foreign", "multi_select_theme", "open_answer", "fill_in_sentence"],
            10, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.Equal(10, saved.Count);
    }

    // ---- New question types ----

    [Fact]
    public async Task RunAsync_MatchingPairs_ProducesGroupQuestionWithNullWordId()
    {
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 6);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["matching_pairs"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        var matching = saved.Where(q => q.QuestionType == Question.QuestionTypes.MatchingPairs).ToList();
        Assert.Single(matching); // 5 questions requested → at most 5/5 = 1 group
        Assert.Null(matching[0].WordId);
        await _aiProvider.DidNotReceive().GenerateFillSentencesAsync(
            Arg.Any<IReadOnlyList<FillSentenceRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ListenAndScramble_SucceedWithoutAnyAiCall()
    {
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 6); // "term1".."term6" — scramble-eligible (5 chars, single token)
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(
            Guid.NewGuid(), userId, [block.Id], ["listen_and_type", "word_scramble"], 6, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.Contains(saved, q => q.QuestionType == Question.QuestionTypes.ListenAndType);
        Assert.Contains(saved, q => q.QuestionType == Question.QuestionTypes.WordScramble);
        await _aiProvider.DidNotReceive().GenerateFillSentencesAsync(
            Arg.Any<IReadOnlyList<FillSentenceRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _aiProvider.DidNotReceive().GenerateDefinitionsAsync(
            Arg.Any<IReadOnlyList<DefinitionRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WordScramble_SkipsMultiWordTerms()
    {
        var userId = Guid.NewGuid();
        var block = new WordBlock(userId, 1, "Test Block");
        _blockRepository.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        var words = Enumerable.Range(1, 6)
            .Select(i => new Word(block.Id, $"multi word term {i}", $"переклад{i}", Word.WordTypes.Phrase))
            .ToList();
        _wordRepository.GetByBlockIdsAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(block.Id)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Word>>(words));

        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["word_scramble"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.NotEmpty(saved);
        Assert.DoesNotContain(saved, q => q.QuestionType == Question.QuestionTypes.WordScramble);
    }

    [Fact]
    public async Task RunAsync_DefinitionMatch_GeneratesAndCachesDefinitionOnWord()
    {
        var userId = Guid.NewGuid();
        var (block, words) = SeedBlockWithWords(userId, 5);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        _aiProvider.GenerateDefinitionsAsync(
                Arg.Any<IReadOnlyList<DefinitionRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var requests = callInfo.Arg<IReadOnlyList<DefinitionRequest>>();
                IReadOnlyList<DefinitionAtom> atoms = requests
                    .Select(r => new DefinitionAtom(r.WordId, "A concept learners study to expand their vocabulary."))
                    .ToList();
                return Task.FromResult(atoms);
            });

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["definition_match"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.Contains(saved, q => q.QuestionType == Question.QuestionTypes.DefinitionMatch);
        Assert.Contains(words, w => !string.IsNullOrEmpty(w.Definition));
    }

    [Fact]
    public async Task RunAsync_DefinitionsAllFail_FallsBackAndStillDeliversQuestions()
    {
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 5);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        _aiProvider.GenerateDefinitionsAsync(
                Arg.Any<IReadOnlyList<DefinitionRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<DefinitionAtom>>([]));

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["definition_match"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.Equal(5, saved.Count);
        Assert.DoesNotContain(saved, q => q.QuestionType == Question.QuestionTypes.DefinitionMatch);
    }

    [Fact]
    public async Task RunAsync_SentenceBuilder_ReusesCachedExampleSentenceWithoutAiCall()
    {
        var userId = Guid.NewGuid();
        var block = new WordBlock(userId, 1, "Test Block");
        _blockRepository.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        var words = Enumerable.Range(1, 5)
            .Select(i => new Word(block.Id, $"term{i}", $"переклад{i}",
                exampleSentence: $"Everyone should learn the word term{i} during class today."))
            .ToList();
        _wordRepository.GetByBlockIdsAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(block.Id)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Word>>(words));

        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(Guid.NewGuid(), userId, [block.Id], ["sentence_builder"], 5, CancellationToken.None);

        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.Contains(saved, q => q.QuestionType == Question.QuestionTypes.SentenceBuilder);
        await _aiProvider.DidNotReceive().GenerateFillSentencesAsync(
            Arg.Any<IReadOnlyList<FillSentenceRequest>>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_SavedQuestions_HaveContiguousSortOrderAfterShuffle()
    {
        var userId = Guid.NewGuid();
        var (block, _) = SeedBlockWithWords(userId, 6);
        var test = Test.Create(userId, "Test");
        _testRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(test);
        var saved = CaptureSavedQuestions();

        await _job.RunAsync(
            Guid.NewGuid(), userId, [block.Id],
            ["translate_to_native", "open_answer"], 8, CancellationToken.None);

        var sortOrders = saved.Select(q => q.SortOrder).OrderBy(x => x).ToList();
        Assert.Equal(Enumerable.Range(0, saved.Count), sortOrders);
    }
}
