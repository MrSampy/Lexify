using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Application.Words.Commands.BulkDeleteWords;
using Lexify.Application.Words.Commands.BulkMoveWords;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Words;

public class BulkWordCommandTests
{
    private readonly IWordBlockRepository _blockRepo = Substitute.For<IWordBlockRepository>();
    private readonly IWordRepository _wordRepo = Substitute.For<IWordRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private static List<Word> BuildWords(Guid blockId, int count) =>
        Enumerable.Range(1, count)
            .Select(i => Word.Create(blockId, $"term{i}", $"trans{i}"))
            .ToList();

    // ---- BulkDelete ----

    [Fact]
    public async Task BulkDelete_ForeignBlock_ReturnsForbidden()
    {
        var blockId = Guid.NewGuid();
        _currentUser.UserId.Returns(Guid.NewGuid());
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(Guid.NewGuid(), 1, "Foreign"));

        var handler = new BulkDeleteWordsCommandHandler(_wordRepo, _blockRepo, _currentUser);
        var result = await handler.Handle(
            new BulkDeleteWordsCommand(blockId, [Guid.NewGuid()]), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task BulkDelete_UnknownBlock_ReturnsNotFound()
    {
        _blockRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WordBlock?)null);

        var handler = new BulkDeleteWordsCommandHandler(_wordRepo, _blockRepo, _currentUser);
        var result = await handler.Handle(
            new BulkDeleteWordsCommand(Guid.NewGuid(), [Guid.NewGuid()]), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task BulkDelete_ForeignWordIds_AreSilentlyDropped()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var ownWords = BuildWords(blockId, 2);

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(userId, 1, "Mine"));
        // Repository scoping only returns words that belong to the block.
        _wordRepo.GetByIdsInBlockAsync(blockId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ownWords);

        var handler = new BulkDeleteWordsCommandHandler(_wordRepo, _blockRepo, _currentUser);
        var requestedIds = ownWords.Select(w => w.Id).Append(Guid.NewGuid()).ToList(); // + foreign id
        var result = await handler.Handle(
            new BulkDeleteWordsCommand(blockId, requestedIds), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        await _wordRepo.Received(1).DeleteRangeAsync(ownWords, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkDelete_NoMatchingWords_ReturnsZeroWithoutDeleting()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(userId, 1, "Mine"));
        _wordRepo.GetByIdsInBlockAsync(blockId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new BulkDeleteWordsCommandHandler(_wordRepo, _blockRepo, _currentUser);
        var result = await handler.Handle(
            new BulkDeleteWordsCommand(blockId, [Guid.NewGuid()]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        await _wordRepo.DidNotReceive().DeleteRangeAsync(Arg.Any<IEnumerable<Word>>(), Arg.Any<CancellationToken>());
    }

    // ---- BulkMove ----

    [Fact]
    public async Task BulkMove_SameSourceAndTarget_ReturnsFailure()
    {
        var blockId = Guid.NewGuid();
        var handler = new BulkMoveWordsCommandHandler(_wordRepo, _blockRepo, _currentUser);
        var result = await handler.Handle(
            new BulkMoveWordsCommand(blockId, blockId, [Guid.NewGuid()]), CancellationToken.None);

        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    [Fact]
    public async Task BulkMove_LanguageMismatch_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(sourceId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(userId, 1, "English block"));
        _blockRepo.GetByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(userId, 5, "German block"));

        var handler = new BulkMoveWordsCommandHandler(_wordRepo, _blockRepo, _currentUser);
        var result = await handler.Handle(
            new BulkMoveWordsCommand(sourceId, targetId, [Guid.NewGuid()]), CancellationToken.None);

        Assert.Equal(ResultStatus.Failure, result.Status);
        await _wordRepo.DidNotReceive().UpdateAsync(Arg.Any<Word>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkMove_ForeignTargetBlock_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(sourceId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(userId, 1, "Mine"));
        _blockRepo.GetByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(Guid.NewGuid(), 1, "Someone else's"));

        var handler = new BulkMoveWordsCommandHandler(_wordRepo, _blockRepo, _currentUser);
        var result = await handler.Handle(
            new BulkMoveWordsCommand(sourceId, targetId, [Guid.NewGuid()]), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task BulkMove_HappyPath_MovesAllWordsAndReturnsCount()
    {
        var userId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var words = BuildWords(sourceId, 3);

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(sourceId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(userId, 1, "Source"));
        _blockRepo.GetByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(userId, 1, "Target"));
        _wordRepo.GetByIdsInBlockAsync(sourceId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(words);

        var handler = new BulkMoveWordsCommandHandler(_wordRepo, _blockRepo, _currentUser);
        var result = await handler.Handle(
            new BulkMoveWordsCommand(sourceId, targetId, words.Select(w => w.Id).ToList()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
        Assert.All(words, w => Assert.Equal(targetId, w.BlockId));
        await _wordRepo.Received(3).UpdateAsync(Arg.Any<Word>(), Arg.Any<CancellationToken>());
    }
}
