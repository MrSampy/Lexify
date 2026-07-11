using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Application.Words.Commands.UpdateWord;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Words;

public class UpdateWordCommandTests
{
    private readonly IWordRepository _wordRepo = Substitute.For<IWordRepository>();
    private readonly IWordBlockRepository _blockRepo = Substitute.For<IWordBlockRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly UpdateWordCommandHandler _handler;

    public UpdateWordCommandTests()
    {
        _handler = new UpdateWordCommandHandler(_wordRepo, _blockRepo, _currentUser);
    }

    private (Word word, Guid userId) ArrangeOwnedWord()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var word = new Word(blockId, "unwind", "розслабитися");
        var block = new WordBlock(userId, 1, "Test Block");

        _currentUser.UserId.Returns(userId);
        _wordRepo.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);
        return (word, userId);
    }

    [Fact]
    public async Task Handle_AddsSynonymsToExistingWord_AndPersists()
    {
        var (word, _) = ArrangeOwnedWord();

        var command = new UpdateWordCommand(
            word.Id, "розслабитися", null, null, false, null,
            Synonyms: ["relax", "chill"]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["relax", "chill"], word.Synonyms);
        await _wordRepo.Received(1).UpdateAsync(word, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullSynonyms_LeavesExistingSynonymsUnchanged()
    {
        var (word, _) = ArrangeOwnedWord();
        word.SetSynonyms(["relax"]);

        var command = new UpdateWordCommand(
            word.Id, "розслабитися", null, null, false, null,
            Synonyms: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["relax"], word.Synonyms);
    }

    [Fact]
    public async Task Handle_ForeignWord_ReturnsForbidden_AndDoesNotUpdate()
    {
        var word = new Word(Guid.NewGuid(), "unwind", "розслабитися");
        var foreignBlock = new WordBlock(Guid.NewGuid(), 1, "Foreign");

        _currentUser.UserId.Returns(Guid.NewGuid());
        _wordRepo.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blockRepo.GetByIdAsync(word.BlockId, Arg.Any<CancellationToken>()).Returns(foreignBlock);

        var command = new UpdateWordCommand(
            word.Id, "розслабитися", null, null, false, null, Synonyms: ["relax"]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        await _wordRepo.DidNotReceive().UpdateAsync(Arg.Any<Word>(), Arg.Any<CancellationToken>());
    }
}
