using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Application.Common;
using Lexify.Application.Conversations.Common;
using Lexify.Application.Conversations.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Conversations.Commands.EndConversation;

public sealed class EndConversationCommandHandler(
    IConversationRepository conversationRepository,
    IWordRepository wordRepository,
    ILanguageRepository languageRepository,
    IReviewLogRepository reviewLogRepository,
    IAiQuotaService aiQuota,
    IAIProvider aiProvider)
    : IRequestHandler<EndConversationCommand, Result<ConversationSummaryDto>>
{
    // SM-2 quality mapped from the model's usage verdict. A word used correctly counts as a solid recall;
    // used-but-wrong is a lapse (below the recall threshold); a word never used is left untouched — the
    // user simply didn't practise it, which is not evidence of forgetting.
    private const int QualityUsedCorrectly = 4;
    private const int QualityUsedIncorrectly = 2;

    public async Task<Result<ConversationSummaryDto>> Handle(
        EndConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdWithMessagesAsync(request.ConversationId, cancellationToken);
        if (conversation is null || conversation.UserId != request.UserId)
            return Result.NotFound<ConversationSummaryDto>("Conversation not found.");

        if (!conversation.IsActive)
            return Result.Failure<ConversationSummaryDto>("This conversation has already ended.");

        // Atomically claim the end. Two concurrent End requests (double-click, client retry) both pass
        // the IsActive check above; only the one that wins this UPDATE proceeds, so SM-2 is applied once.
        if (!await conversationRepository.TryEndAsync(conversation.Id, cancellationToken))
            return Result.Failure<ConversationSummaryDto>("This conversation has already ended.");
        conversation.End(); // keep the tracked aggregate in sync with the claimed row

        // Load the target words (some may have been deleted since the session started — skip those).
        var words = new List<Word>();
        foreach (var wordId in conversation.TargetWordIds)
        {
            var word = await wordRepository.GetByIdAsync(wordId, cancellationToken);
            if (word is not null) words.Add(word);
        }

        var language = await languageRepository.GetByIdAsync(conversation.LanguageId, cancellationToken);
        var targetLanguage = language?.Name ?? "the target language";

        var history = conversation.Messages
            .OrderBy(m => m.SortOrder)
            .Select(m => new ChatTurn(m.Role, m.Content))
            .ToList();

        var learnerMessages = history
            .Where(t => t.Role == Conversation.Roles.User)
            .Select(t => t.Content)
            .ToList();
        var learnerText = ConversationScoring.Normalize(string.Join(" ", learnerMessages));

        var targetWords = words.Select(w => new TargetWord(w.Id, w.Term, w.Translation)).ToList();

        // The LLM verdict is only a secondary signal for CORRECTNESS. Whether a word was USED is decided
        // deterministically from the learner's own turns (matches the client chips) — the model routinely
        // under-reports usage, which was the "I used it but it said I didn't" bug. When the daily AI
        // quota is spent, the analysis is skipped entirely: the session still ends and scores, the
        // correctness signal just falls back to the generous default.
        var quota = await aiQuota.CheckAsync(request.UserId, cancellationToken);
        var verdicts = quota.IsExceeded
            ? []
            : await aiProvider.AnalyzeConversationAsync(history, targetWords, targetLanguage, cancellationToken);
        // Local models occasionally emit the same word id twice — last one wins (mirrors AIResponseValidator).
        var verdictByWord = verdicts
            .GroupBy(v => v.WordId)
            .ToDictionary(g => g.Key, g => g.Last());

        var results = new List<WordUsageResultDto>(words.Count);
        foreach (var word in words)
        {
            verdictByWord.TryGetValue(word.Id, out var verdict);
            // Usage is decided ONLY deterministically — an LLM verdict must never promote a word the
            // learner didn't actually type into the SM-2 schedule (prompt-injected/hallucinated "used").
            var used = ConversationScoring.IsTermUsed(learnerText, word.Term);
            // Generous default: only an explicit negative verdict downgrades a used word to "incorrect".
            var usedCorrectly = used && (verdict?.UsedCorrectly ?? true);

            int? intervalDays = null;
            DateTimeOffset? nextReviewAt = null;

            if (used)
            {
                var quality = usedCorrectly ? QualityUsedCorrectly : QualityUsedIncorrectly;
                word.ApplyReviewResult(quality);
                await wordRepository.UpdateAsync(word, cancellationToken);
                await reviewLogRepository.AddAsync(
                    new WordReviewLog(
                        request.UserId, word.Id, word.BlockId, conversation.LanguageId,
                        quality, WordReviewLog.Sources.Conversation,
                        word.EaseFactor, word.IntervalDays),
                    cancellationToken);

                intervalDays = word.IntervalDays;
                nextReviewAt = word.NextReviewAt;
            }

            results.Add(new WordUsageResultDto(
                word.Id, word.Term, word.Translation,
                used, usedCorrectly, verdict?.Note,
                intervalDays, nextReviewAt));
        }

        var score = ConversationScoring.Compute(
            words.Select(w => w.Term).ToList(),
            learnerMessages,
            finalUsedCount: results.Count(r => r.Used));

        // Persist the score so the history list can show it without replaying transcripts.
        conversation.RecordScore(score.Points, score.Stars, score.WordsUsed);
        await conversationRepository.UpdateAsync(conversation, cancellationToken);

        return Result.Ok(new ConversationSummaryDto(
            results,
            new ConversationScoreDto(
                score.Points, score.Stars, score.WordsUsed,
                score.TotalWords, score.MessagesUsed, score.MessageBudget)));
    }
}
