using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Application.Common;
using Lexify.Application.Conversations.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Conversations.Commands.StartConversation;

public sealed class StartConversationCommandHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    ILanguageRepository languageRepository,
    IUserRepository userRepository,
    IConversationRepository conversationRepository,
    IAiQuotaService aiQuota,
    IAIProvider aiProvider)
    : IRequestHandler<StartConversationCommand, Result<StartConversationResultDto>>
{
    /// <summary>How many words a single session tries to weave in — enough to be useful, few enough to fit a short chat.</summary>
    private const int TargetWordCount = 6;

    public async Task<Result<StartConversationResultDto>> Handle(
        StartConversationCommand request, CancellationToken cancellationToken)
    {
        var quota = await aiQuota.CheckAsync(request.UserId, cancellationToken);
        if (quota.IsExceeded)
            return Result.Failure<StartConversationResultDto>(
                $"Daily AI limit reached ({quota.Used}/{quota.Limit}). It resets at midnight UTC.");

        // If a block is scoped, guarantee content by cramming (ignore schedule); otherwise practise what's
        // due, and fall back to cram across everything so "nothing due today" doesn't dead-end the feature.
        var words = await SelectTargetWordsAsync(request.UserId, request.BlockId, cancellationToken);
        if (words.Count == 0)
            return Result.Failure<StartConversationResultDto>(
                "No words to practise yet. Add some words to a block first.");

        // A conversation is single-language: keep only the words in the majority language.
        var languageIds = await blockRepository.GetLanguageIdsAsync(
            words.Select(w => w.BlockId).Distinct().ToArray(), cancellationToken);

        var languageId = languageIds
            .GroupBy(kv => kv.Value)
            .OrderByDescending(g => g.Count())
            .First().Key;

        words = words
            .Where(w => languageIds.TryGetValue(w.BlockId, out var id) && id == languageId)
            .Take(TargetWordCount)
            .ToList();

        var language = await languageRepository.GetByIdAsync(languageId, cancellationToken);
        var targetLanguage = language?.Name ?? "the target language";

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        var targetWords = words
            .Select(w => new TargetWord(w.Id, w.Term, w.Translation))
            .ToList();

        var context = new ChatContext(
            TargetLanguage: targetLanguage,
            NativeLanguage: request.NativeLanguage,
            CefrLevel: user?.EnglishLevel,
            Scenario: request.Scenario,
            TargetWords: targetWords);

        // Opening line: accumulate the streamed reply into a single message (this endpoint is not itself SSE).
        var openingBuilder = new StringBuilder();
        await foreach (var chunk in aiProvider.StreamChatReplyAsync(context, [], cancellationToken))
            openingBuilder.Append(chunk);

        var opening = openingBuilder.ToString().Trim();
        if (opening.Length == 0)
            opening = "Hi! I'm Lexi. Let's practise together — just write back whenever you're ready.";

        var title = request.Scenario is { Length: > 0 } s ? s : $"Chat • {targetLanguage}";

        var conversation = Conversation.Create(
            request.UserId, languageId, title, request.Scenario, targetWords.Select(t => t.WordId));
        conversation.AddMessage(Conversation.Roles.Assistant, opening);

        await conversationRepository.AddAsync(conversation, cancellationToken);

        return Result.Ok(new StartConversationResultDto(
            ConversationId: conversation.Id,
            LanguageId: languageId,
            TargetLanguage: targetLanguage,
            Title: title,
            Scenario: request.Scenario,
            TargetWords: targetWords.Select(t => new TargetWordDto(t.WordId, t.Term, t.Translation)).ToList(),
            OpeningMessage: opening,
            MessageBudget: Common.ConversationScoring.BudgetFor(targetWords.Count)));
    }

    private async Task<List<Word>> SelectTargetWordsAsync(Guid userId, Guid? blockId, CancellationToken ct)
    {
        if (blockId is { } bId)
        {
            var due = await wordRepository.GetDueForReviewAsync(userId, TargetWordCount, bId, cram: false, ct);
            if (due.Count >= 3) return due.ToList();
            return (await wordRepository.GetDueForReviewAsync(userId, TargetWordCount, bId, cram: true, ct)).ToList();
        }

        var dueGlobal = await wordRepository.GetDueForReviewAsync(userId, TargetWordCount, null, cram: false, ct);
        if (dueGlobal.Count > 0) return dueGlobal.ToList();

        return (await wordRepository.GetDueForReviewAsync(userId, TargetWordCount, null, cram: true, ct)).ToList();
    }
}
