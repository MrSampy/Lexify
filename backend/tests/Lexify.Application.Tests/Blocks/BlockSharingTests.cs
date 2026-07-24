using Lexify.Application.Abstractions;
using Lexify.Application.Blocks.Commands.CopySharedBlock;
using Lexify.Application.Blocks.Commands.CreateBlockShare;
using Lexify.Application.Blocks.Commands.RevokeBlockShare;
using Lexify.Application.Blocks.Queries.GetSharedBlock;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Blocks;

public class BlockSharingTests
{
    private readonly IWordBlockRepository _blockRepo = Substitute.For<IWordBlockRepository>();
    private readonly IBlockShareRepository _shareRepo = Substitute.For<IBlockShareRepository>();
    private readonly IWordRepository _wordRepo = Substitute.For<IWordRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // ---- CreateBlockShare ----

    [Fact]
    public async Task CreateShare_ForeignBlock_ReturnsForbidden()
    {
        var blockId = Guid.NewGuid();
        _currentUser.UserId.Returns(Guid.NewGuid());
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(Guid.NewGuid(), 1, "Someone else's"));

        var handler = new CreateBlockShareCommandHandler(_blockRepo, _shareRepo, _currentUser);
        var result = await handler.Handle(new CreateBlockShareCommand(blockId), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        await _shareRepo.DidNotReceive().AddAsync(Arg.Any<BlockShare>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShare_WhenAlreadyShared_ReusesTheSameLink()
    {
        // Pressing "share" twice must not invalidate the link the owner already pasted somewhere.
        var userId = Guid.NewGuid();
        var block = new WordBlock(userId, 1, "Mine");
        var existing = new BlockShare(block.Id, userId, "existing-token");

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        _shareRepo.GetActiveByBlockIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new CreateBlockShareCommandHandler(_blockRepo, _shareRepo, _currentUser);
        var result = await handler.Handle(new CreateBlockShareCommand(block.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("existing-token", result.Value!.Token);
        await _shareRepo.DidNotReceive().AddAsync(Arg.Any<BlockShare>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShare_WhenNotShared_MintsAToken()
    {
        var userId = Guid.NewGuid();
        var block = new WordBlock(userId, 1, "Mine");

        _currentUser.UserId.Returns(userId);
        _blockRepo.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        _shareRepo.GetActiveByBlockIdAsync(block.Id, Arg.Any<CancellationToken>())
            .Returns((BlockShare?)null);

        var handler = new CreateBlockShareCommandHandler(_blockRepo, _shareRepo, _currentUser);
        var result = await handler.Handle(new CreateBlockShareCommand(block.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.Token));
        // URL-safe: the token goes straight into a path segment, unescaped.
        Assert.DoesNotContain('+', result.Value.Token);
        Assert.DoesNotContain('/', result.Value.Token);
        Assert.DoesNotContain('=', result.Value.Token);
        await _shareRepo.Received(1).AddAsync(Arg.Any<BlockShare>(), Arg.Any<CancellationToken>());
    }

    // ---- RevokeBlockShare ----

    [Fact]
    public async Task RevokeShare_ForeignBlock_ReturnsForbidden()
    {
        var blockId = Guid.NewGuid();
        _currentUser.UserId.Returns(Guid.NewGuid());
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>())
            .Returns(new WordBlock(Guid.NewGuid(), 1, "Someone else's"));

        var handler = new RevokeBlockShareCommandHandler(_blockRepo, _shareRepo, _currentUser);
        var result = await handler.Handle(new RevokeBlockShareCommand(blockId), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    // ---- GetSharedBlock ----

    [Fact]
    public async Task GetShared_RevokedToken_ReturnsNotFound()
    {
        var share = new BlockShare(Guid.NewGuid(), Guid.NewGuid(), "token");
        share.Revoke();
        _shareRepo.GetByTokenAsync("token", Arg.Any<CancellationToken>()).Returns(share);

        var handler = new GetSharedBlockQueryHandler(_shareRepo, _blockRepo, _wordRepo, _userRepo);
        var result = await handler.Handle(new GetSharedBlockQuery("token"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetShared_UnknownToken_ReturnsNotFound()
    {
        _shareRepo.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((BlockShare?)null);

        var handler = new GetSharedBlockQueryHandler(_shareRepo, _blockRepo, _wordRepo, _userRepo);
        var result = await handler.Handle(new GetSharedBlockQuery("made-up"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetShared_ReturnsWordsWithoutOwnerProgress()
    {
        var owner = new User("owner@example.com", "hash", "Owner");
        var block = new WordBlock(Guid.NewGuid(), 1, "Shared block", "A description");
        var word = Word.Create(block.Id, "term", "translation", Word.WordTypes.Word, "notes", "example");
        word.SetAlternativeTranslations(["alt"]);
        word.SetSynonyms(["syn"]);
        var share = new BlockShare(block.Id, block.UserId, "token");

        _shareRepo.GetByTokenAsync("token", Arg.Any<CancellationToken>()).Returns(share);
        _blockRepo.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        _userRepo.GetByIdAsync(block.UserId, Arg.Any<CancellationToken>()).Returns(owner);
        _wordRepo.GetByBlockIdAsync(
                block.Id, Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([word]);

        var handler = new GetSharedBlockQueryHandler(_shareRepo, _blockRepo, _wordRepo, _userRepo);
        var result = await handler.Handle(new GetSharedBlockQuery("token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Shared block", result.Value!.Title);
        Assert.Equal("Owner", result.Value.OwnerDisplayName);

        var shared = Assert.Single(result.Value.Words);
        Assert.Equal("term", shared.Term);
        Assert.Equal(["alt"], shared.AlternativeTranslations);
        Assert.Equal(["syn"], shared.Synonyms);
        // The DTO carries no SM-2 fields at all — the owner's study history is not part of the
        // vocabulary being shared. This asserts the shape stays that way.
        Assert.DoesNotContain(
            typeof(Lexify.Application.Blocks.Common.SharedWordDto).GetProperties(),
            p => p.Name.Contains("Ease") || p.Name.Contains("Interval") || p.Name.Contains("Review"));
    }

    // ---- CopySharedBlock ----

    [Fact]
    public async Task Copy_RevokedToken_ReturnsNotFound()
    {
        var share = new BlockShare(Guid.NewGuid(), Guid.NewGuid(), "token");
        share.Revoke();
        _shareRepo.GetByTokenAsync("token", Arg.Any<CancellationToken>()).Returns(share);

        var handler = new CopySharedBlockCommandHandler(
            _shareRepo, _blockRepo, _wordRepo, _currentUser, _unitOfWork);
        var result = await handler.Handle(new CopySharedBlockCommand("token"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        await _blockRepo.DidNotReceive().AddAsync(Arg.Any<WordBlock>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Copy_ClonesWordsIntoCallersAccountWithFreshProgress()
    {
        var recipientId = Guid.NewGuid();
        var block = new WordBlock(Guid.NewGuid(), 3, "Norwegian basics", "Starter pack");
        var share = new BlockShare(block.Id, block.UserId, "token");

        var original = Word.Create(block.Id, "hei", "hello", Word.WordTypes.Word, "greeting", "Hei!");
        original.SetAlternativeTranslations(["hi"]);
        original.SetSynonyms(["hallo"]);
        // Give the source word a review history the copy must not inherit.
        original.ApplyReviewResult(5);

        _currentUser.UserId.Returns(recipientId);
        _shareRepo.GetByTokenAsync("token", Arg.Any<CancellationToken>()).Returns(share);
        _blockRepo.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        _wordRepo.GetByBlockIdAsync(
                block.Id, Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([original]);

        WordBlock? created = null;
        _blockRepo
            .When(r => r.AddAsync(Arg.Any<WordBlock>(), Arg.Any<CancellationToken>()))
            .Do(call => created = call.Arg<WordBlock>());

        List<Word>? copiedWords = null;
        _wordRepo
            .When(r => r.AddRangeAsync(Arg.Any<IEnumerable<Word>>(), Arg.Any<CancellationToken>()))
            .Do(call => copiedWords = [.. call.Arg<IEnumerable<Word>>()]);

        var handler = new CopySharedBlockCommandHandler(
            _shareRepo, _blockRepo, _wordRepo, _currentUser, _unitOfWork);
        var result = await handler.Handle(new CopySharedBlockCommand("token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(created);
        Assert.Equal(recipientId, created!.UserId);
        Assert.Equal(block.LanguageId, created.LanguageId);
        Assert.Equal("Norwegian basics (copy)", created.Title);

        var copy = Assert.Single(copiedWords!);
        Assert.Equal(created.Id, copy.BlockId);
        Assert.Equal("hei", copy.Term);
        Assert.Equal("hello", copy.Translation);
        Assert.Equal(["hi"], copy.AlternativeTranslations);
        Assert.Equal(["hallo"], copy.Synonyms);
        Assert.Equal("greeting", copy.Notes);
        Assert.Equal("Hei!", copy.ExampleSentence);

        // The point of copying rather than sharing a row: the recipient starts from zero.
        Assert.Equal(0, copy.Repetitions);
        Assert.Equal(2.5, copy.EaseFactor);
        Assert.Null(copy.LastReviewedAt);
        Assert.NotEqual(original.Repetitions, copy.Repetitions);

        // The block flushes before the words so they have a real block id to reference.
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
