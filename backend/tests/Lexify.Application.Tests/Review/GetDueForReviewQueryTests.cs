using AutoMapper;
using Lexify.Application.Review.Queries.GetDueForReview;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Review;

public class GetDueForReviewQueryTests
{
    private readonly IWordRepository _wordRepo;
    private readonly IWordBlockRepository _blockRepo;
    private readonly IUserRepository _userRepo;
    private readonly IReviewLogRepository _reviewLogRepo;
    private readonly GetDueForReviewQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _blockId = Guid.NewGuid();

    public GetDueForReviewQueryTests()
    {
        _wordRepo = Substitute.For<IWordRepository>();
        _blockRepo = Substitute.For<IWordBlockRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _reviewLogRepo = Substitute.For<IReviewLogRepository>();

        var mapper = Substitute.For<IMapper>();
        mapper.Map<IReadOnlyList<WordDto>>(Arg.Any<object>()).Returns([]);

        _blockRepo.GetLanguageIdsAsync(Arg.Any<Guid[]>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, short>());

        _handler = new GetDueForReviewQueryHandler(_wordRepo, _blockRepo, _userRepo, _reviewLogRepo, mapper);
    }

    private User CreateUser(int newWordsPerDay)
    {
        var user = User.Create("u@example.com", "hash");
        user.SetNewWordsPerDay(newWordsPerDay);
        _userRepo.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private static Word NewWord(Guid blockId) => Word.Create(blockId, "term", "translation");

    private static Word ReviewedWord(Guid blockId)
    {
        var word = Word.Create(blockId, "term", "translation");
        word.ApplyReviewResult(4);
        return word;
    }

    [Fact]
    public async Task Handle_PassesRemainingNewWordAllowanceToRepository()
    {
        CreateUser(newWordsPerDay: 10);
        _reviewLogRepo.CountNewWordsIntroducedSinceAsync(_userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(3);
        _wordRepo.GetReviewQueueAsync(_userId, 20, 7, null, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new GetDueForReviewQuery(_userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _wordRepo.Received(1).GetReviewQueueAsync(_userId, 20, 7, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetExhausted_AllowanceClampedToZero()
    {
        CreateUser(newWordsPerDay: 5);
        _reviewLogRepo.CountNewWordsIntroducedSinceAsync(_userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(9);
        _wordRepo.GetReviewQueueAsync(_userId, 20, 0, null, Arg.Any<CancellationToken>())
            .Returns([]);

        await _handler.Handle(new GetDueForReviewQuery(_userId), CancellationToken.None);

        await _wordRepo.Received(1).GetReviewQueueAsync(_userId, 20, 0, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReportsQueueComposition()
    {
        CreateUser(newWordsPerDay: 10);
        _reviewLogRepo.CountNewWordsIntroducedSinceAsync(_userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(2);
        IReadOnlyList<Word> queue = [ReviewedWord(_blockId), ReviewedWord(_blockId), NewWord(_blockId)];
        _wordRepo.GetReviewQueueAsync(_userId, 20, 8, null, Arg.Any<CancellationToken>())
            .Returns(queue);

        var result = await _handler.Handle(new GetDueForReviewQuery(_userId), CancellationToken.None);

        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.NewCount);
        Assert.Equal(2, result.Value.ReviewCount);
        Assert.Equal(10, result.Value.NewLimit);
        Assert.Equal(2, result.Value.NewIntroducedToday);
    }

    [Fact]
    public async Task Handle_Cram_IgnoresNewWordBudget()
    {
        CreateUser(newWordsPerDay: 0);
        _reviewLogRepo.CountNewWordsIntroducedSinceAsync(_userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        IReadOnlyList<Word> all = [NewWord(_blockId), ReviewedWord(_blockId)];
        _wordRepo.GetDueForReviewAsync(_userId, 500, null, true, Arg.Any<CancellationToken>())
            .Returns(all);

        var result = await _handler.Handle(
            new GetDueForReviewQuery(_userId, Limit: 500, Cram: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _wordRepo.DidNotReceiveWithAnyArgs()
            .GetReviewQueueAsync(default, default, default, default, default);
        Assert.Equal(1, result.Value!.NewCount);
        Assert.Equal(1, result.Value.ReviewCount);
    }
}
