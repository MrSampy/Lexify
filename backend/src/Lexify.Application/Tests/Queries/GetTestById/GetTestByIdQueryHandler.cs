using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tests.Queries.GetTestById;

public sealed class GetTestByIdQueryHandler(
    ITestRepository testRepository,
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository)
    : IRequestHandler<GetTestByIdQuery, Result<TestDto>>
{
    public async Task<Result<TestDto>> Handle(GetTestByIdQuery request, CancellationToken cancellationToken)
    {
        var test = await testRepository.GetByIdWithQuestionsAsync(request.TestId, cancellationToken);
        if (test is null)
            return Result.NotFound<TestDto>("Test not found.");

        if (test.UserId != request.UserId)
            return Result.Forbidden<TestDto>("You do not have access to this test.");

        // listen_and_type needs the word's language so the client can pronounce it (server neural
        // audio by wordId+languageId, browser voice by languageId). Resolve just those words'
        // languages — a small subset — caching block lookups so repeated blocks cost one query.
        var languageByWordId = await ResolveListenLanguagesAsync(test, cancellationToken);

        var questionDtos = test.Questions
            .OrderBy(q => q.SortOrder)
            .Select(q =>
            {
                var isListen = q.QuestionType == Question.QuestionTypes.ListenAndType;
                int? languageId = isListen && q.WordId is { } wid && languageByWordId.TryGetValue(wid, out var lid)
                    ? lid
                    : null;
                return new QuestionDto(
                    q.Id,
                    q.QuestionType,
                    q.QuestionText,
                    q.SortOrder,
                    q.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new QuestionOptionDto(o.Id, o.OptionText, o.SortOrder))
                        .ToList(),
                    isListen ? q.CorrectAnswer : null,
                    isListen ? q.WordId : null,
                    languageId);
            })
            .ToList();

        var dto = new TestDto(
            test.Id, test.Title, test.Status, test.QuestionCount, test.CreatedAt, questionDtos);

        return Result.Ok(dto);
    }

    private async Task<Dictionary<Guid, int>> ResolveListenLanguagesAsync(Test test, CancellationToken ct)
    {
        var wordIds = test.Questions
            .Where(q => q.QuestionType == Question.QuestionTypes.ListenAndType && q.WordId.HasValue)
            .Select(q => q.WordId!.Value)
            .Distinct()
            .ToList();

        var result = new Dictionary<Guid, int>();
        if (wordIds.Count == 0) return result;

        var languageByBlockId = new Dictionary<Guid, int?>();
        foreach (var wordId in wordIds)
        {
            var word = await wordRepository.GetByIdAsync(wordId, ct);
            if (word is null) continue;

            if (!languageByBlockId.TryGetValue(word.BlockId, out var languageId))
            {
                var block = await blockRepository.GetByIdAsync(word.BlockId, ct);
                languageId = block?.LanguageId;
                languageByBlockId[word.BlockId] = languageId;
            }

            if (languageId is { } lid) result[wordId] = lid;
        }

        return result;
    }
}
