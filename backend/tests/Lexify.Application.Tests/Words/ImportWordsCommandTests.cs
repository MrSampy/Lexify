using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Application.Words.Commands.ImportWords;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Words;

public class ImportWordsCommandTests
{
    private readonly IWordBlockRepository _blockRepo;
    private readonly IWordRepository _wordRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ImportWordsCommandHandler _handler;

    public ImportWordsCommandTests()
    {
        _blockRepo = Substitute.For<IWordBlockRepository>();
        _wordRepo = Substitute.For<IWordRepository>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _handler = new ImportWordsCommandHandler(_blockRepo, _wordRepo, _currentUser);
    }

    private static List<ImportWordItem> BuildWords(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new ImportWordItem($"term{i}", $"trans{i}", "word", null, null, false, null))
            .ToList();

    [Fact]
    public async Task Handle_ValidImport_ReturnsFiveWords()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var block = new WordBlock(userId, 1, "Test Block");

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);

        var command = new ImportWordsCommand(blockId, BuildWords(5));
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
        await _wordRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Word>>(w => w.Count() == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForeignBlock_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var block = new WordBlock(Guid.NewGuid(), 1, "Foreign Block");

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);

        var command = new ImportWordsCommand(blockId, BuildWords(1));
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_NonExistentBlock_ReturnsNotFound()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _blockRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WordBlock?)null);

        var command = new ImportWordsCommand(Guid.NewGuid(), BuildWords(1));
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public void Validator_MoreThan200Words_HasValidationError()
    {
        var validator = new ImportWordsCommandValidator();

        var result = validator.Validate(new ImportWordsCommand(Guid.NewGuid(), BuildWords(201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("200"));
    }
}
