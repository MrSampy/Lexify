using Lexify.Application.Common;
using Lexify.Application.Review.Commands.ReviewWord;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Review;

public class ReviewWordCommandTests
{
    private readonly IWordRepository _wordRepo;
    private readonly IWordBlockRepository _blockRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ReviewWordCommandHandler _handler;

    public ReviewWordCommandTests()
    {
        _wordRepo = Substitute.For<IWordRepository>();
        _blockRepo = Substitute.For<IWordBlockRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new ReviewWordCommandHandler(_wordRepo, _blockRepo, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Quality5_EaseFactorIncreased()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var word = new Word(blockId, "apple", "яблуко");
        var block = new WordBlock(userId, 1, "Block");

        _wordRepo.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);

        var result = await _handler.Handle(new ReviewWordCommand(word.Id, userId, 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2.6, word.EaseFactor, precision: 5);
        Assert.Equal(1, word.Repetitions);
    }

    [Fact]
    public async Task Handle_Quality0_RepetitionsResetToZero()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var word = new Word(blockId, "apple", "яблуко");
        var block = new WordBlock(userId, 1, "Block");

        word.ApplyReviewResult(5); // advance repetitions to 1
        word.ClearDomainEvents();

        _wordRepo.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);

        var result = await _handler.Handle(new ReviewWordCommand(word.Id, userId, 0), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, word.Repetitions);
        Assert.Equal(1, word.IntervalDays);
    }

    [Fact]
    public async Task Handle_ForeignBlock_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var word = new Word(blockId, "apple", "яблуко");
        var block = new WordBlock(Guid.NewGuid(), 1, "Foreign Block");

        _wordRepo.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);

        var result = await _handler.Handle(new ReviewWordCommand(word.Id, userId, 5), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }
}
