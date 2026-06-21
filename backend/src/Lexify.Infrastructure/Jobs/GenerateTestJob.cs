using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class GenerateTestJob(
    IWordRepository wordRepository,
    ITestRepository testRepository,
    IQuestionRepository questionRepository,
    IAIProvider aiProvider,
    IUnitOfWork unitOfWork,
    ILogger<GenerateTestJob> logger)
{
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

        // Load all words from the selected blocks
        var words = await wordRepository.GetByBlockIdsAsync(blockIds, cancellationToken);
        if (words.Count < 5)
        {
            LogInsufficientWords(logger, testId, words.Count);
            return;
        }

        // Load hashes already used in this user's tests (deduplication)
        var usedHashes = await questionRepository.GetUsedContentHashesByUserAsync(userId, cancellationToken);

        // Map Word entities → WordDto (what IAIProvider expects)
        var wordDtos = words
            .Select(w => new WordDto(
                w.Id, w.BlockId, w.Term, w.Translation, w.WordType,
                w.Notes, w.ExampleSentence, w.ConfidenceFlag, w.ConfidenceNote,
                w.SortOrder, w.CreatedAt, w.EaseFactor, w.IntervalDays, w.Repetitions, w.NextReviewAt))
            .ToList();

        // Build per-term lookup for linking questions to Word entities
        var wordByTerm = words
            .GroupBy(w => w.Term.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        // Also build a set of all translations for distractor priority (own-block words)
        var blockIdSet = blockIds.ToHashSet();
        var ownBlockTranslations = words
            .Where(w => blockIdSet.Contains(w.BlockId))
            .Select(w => w.Translation)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Call AI provider
        TestGenerationResult aiResult;
        try
        {
            aiResult = await aiProvider.GenerateTestQuestionsAsync(
                wordDtos, questionTypes, questionCount, cancellationToken);
        }
        catch (Exception ex)
        {
            LogAiError(logger, ex, testId);
            return;
        }

        if (aiResult.Questions.Count == 0)
        {
            LogNoQuestionsGenerated(logger, testId);
            return;
        }

        // Map AI questions → domain Question + QuestionOption entities
        var questions = new List<Question>();
        var options = new List<QuestionOption>();
        int sortOrder = 0;

        foreach (var aiQ in aiResult.Questions)
        {
            var domainType = MapToDomainType(aiQ.QuestionType);
            var questionText = aiQ.QuestionType == "fill_blank" && aiQ.FillSentence is not null
                ? aiQ.FillSentence
                : aiQ.Content;
            var correctAnswer = aiQ.Options.FirstOrDefault(o => o.IsCorrect)?.Text
                                ?? aiQ.TargetWordTerm;

            wordByTerm.TryGetValue(aiQ.TargetWordTerm.Trim().ToLowerInvariant(), out var targetWord);

            Question question;
            try
            {
                question = Question.Create(testId, targetWord?.Id, domainType, questionText, correctAnswer, sortOrder++);
            }
            catch (DomainException)
            {
                continue;
            }

            // Skip duplicate questions (already used in a prior test)
            if (usedHashes.Contains(question.ContentHash))
                continue;

            questions.Add(question);

            // Build options:
            // For single_choice / multi_select — supplement AI options with real word distractors if < 4
            if (aiQ.Options.Count > 0)
            {
                var aiOptions = aiQ.Options.ToList();

                // Priority 1: distractors from OTHER blocks (real word translations)
                // Priority 2: from current blocks
                // Priority 3: AI-generated (already present)
                // Only supplement if we have fewer than 4 options total
                if (aiOptions.Count < 4 && domainType is
                    Question.QuestionTypes.TranslateToNative or
                    Question.QuestionTypes.TranslateToForeign or
                    Question.QuestionTypes.MultiSelectTheme)
                {
                    var existingTexts = aiOptions.Select(o => o.Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var distractors = words
                        .Where(w => !string.Equals(w.Term, aiQ.TargetWordTerm, StringComparison.OrdinalIgnoreCase))
                        .Select(w => w.Translation)
                        .Where(t => !existingTexts.Contains(t))
                        .OrderBy(_ => Guid.NewGuid())
                        .Take(4 - aiOptions.Count)
                        .Select(t => new GeneratedOption(t, false));
                    aiOptions.AddRange(distractors);
                }

                int optSort = 0;
                foreach (var opt in aiOptions)
                    options.Add(new QuestionOption(question.Id, opt.Text, opt.IsCorrect, optSort++));
            }
        }

        if (questions.Count == 0)
        {
            LogNoQuestionsGenerated(logger, testId);
            return;
        }

        await questionRepository.AddRangeAsync(questions, cancellationToken);
        await questionRepository.AddOptionsRangeAsync(options, cancellationToken);

        test.MarkReady(questions.Count);
        await testRepository.UpdateAsync(test, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogTestReady(logger, testId, questions.Count);
    }

    private static string MapToDomainType(string aiType) => aiType switch
    {
        "single_choice" => Question.QuestionTypes.TranslateToNative,
        "multi_select"  => Question.QuestionTypes.MultiSelectTheme,
        "fill_blank"    => Question.QuestionTypes.FillInSentence,
        "open_answer"   => Question.QuestionTypes.OpenAnswer,
        // Accept domain types passed through directly
        _ when Question.QuestionTypes.All.Contains(aiType) => aiType,
        _ => Question.QuestionTypes.TranslateToNative
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Test {TestId} not found for generation")]
    private static partial void LogTestNotFound(ILogger logger, Guid testId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Test {TestId} has insufficient words ({Count} < 5)")]
    private static partial void LogInsufficientWords(ILogger logger, Guid testId, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "AI failed to generate questions for test {TestId}")]
    private static partial void LogAiError(ILogger logger, Exception ex, Guid testId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No valid questions generated for test {TestId}")]
    private static partial void LogNoQuestionsGenerated(ILogger logger, Guid testId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Test {TestId} is ready with {Count} questions")]
    private static partial void LogTestReady(ILogger logger, Guid testId, int count);
}
