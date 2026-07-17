using AutoMapper;
using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Review.Queries.GetDueForReview;

public sealed class GetDueForReviewQueryHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    IUserRepository userRepository,
    IReviewLogRepository reviewLogRepository,
    IMapper mapper)
    : IRequestHandler<GetDueForReviewQuery, Result<ReviewQueueDto>>
{
    public async Task<Result<ReviewQueueDto>> Handle(
        GetDueForReviewQuery request, CancellationToken cancellationToken)
    {
        // Introduced-today count keeps the daily new-word budget honest across multiple sessions:
        // words first reviewed since UTC midnight already spent part of the allowance.
        var utcDayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var introducedToday = await reviewLogRepository.CountNewWordsIntroducedSinceAsync(
            request.UserId, utcDayStart, cancellationToken);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var newLimit = user?.NewWordsPerDay ?? User.DefaultNewWordsPerDay;

        IReadOnlyList<Word> words;
        if (request.Cram)
        {
            // Cram ignores the schedule and the new-word budget: the user explicitly asked to
            // practise everything in scope.
            words = await wordRepository.GetDueForReviewAsync(
                request.UserId, request.Limit, request.BlockId, cram: true, cancellationToken);
        }
        else
        {
            var allowance = Math.Max(0, newLimit - introducedToday);
            words = await wordRepository.GetReviewQueueAsync(
                request.UserId, request.Limit, allowance, request.BlockId, cancellationToken);
        }

        // Review cards need the term's language (for TTS voice selection), which lives on the block.
        var languageIds = await blockRepository.GetLanguageIdsAsync(
            words.Select(w => w.BlockId).Distinct().ToArray(), cancellationToken);

        var dtos = mapper.Map<IReadOnlyList<WordDto>>(words)
            .Select(dto => languageIds.TryGetValue(dto.BlockId, out var languageId)
                ? dto with { LanguageId = languageId }
                : dto)
            .ToList();

        var newCount = words.Count(w => w.LastReviewedAt is null);

        return Result.Ok(new ReviewQueueDto(
            Words: dtos,
            NewCount: newCount,
            ReviewCount: words.Count - newCount,
            NewLimit: newLimit,
            NewIntroducedToday: introducedToday));
    }
}
