using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;
using AppGenerateTest = Lexify.Application.Tests.Commands.GenerateTest;

namespace Lexify.Application.Tests.TestHandlers;

public class GenerateTestCommandTests
{
    private readonly IWordBlockRepository _blockRepo;
    private readonly IWordRepository _wordRepo;
    private readonly ITestRepository _testRepo;
    private readonly IBackgroundJobService _bgJobService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppGenerateTest.GenerateTestCommandHandler _handler;

    public GenerateTestCommandTests()
    {
        _blockRepo = Substitute.For<IWordBlockRepository>();
        _wordRepo = Substitute.For<IWordRepository>();
        _testRepo = Substitute.For<ITestRepository>();
        _bgJobService = Substitute.For<IBackgroundJobService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AppGenerateTest.GenerateTestCommandHandler(
            _blockRepo, _wordRepo, _testRepo, _bgJobService, _unitOfWork);
    }

    [Fact]
    public async Task Handle_FewerThan5Words_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var block = new WordBlock(userId, 1, "Block");

        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);
        _wordRepo.CountByBlockIdAsync(blockId, null, Arg.Any<CancellationToken>()).Returns(4);

        var command = new AppGenerateTest.GenerateTestCommand(userId, [blockId], ["open_answer"], 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
        Assert.Contains("5", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ForeignBlockId_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var block = new WordBlock(Guid.NewGuid(), 1, "Block");

        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);

        var command = new AppGenerateTest.GenerateTestCommand(userId, [blockId], ["open_answer"], 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_ValidRequest_EnqueuesJobAndReturnsGeneratingStatus()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var block = new WordBlock(userId, 1, "Block");

        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);
        _wordRepo.CountByBlockIdAsync(blockId, null, Arg.Any<CancellationToken>()).Returns(10);

        var command = new AppGenerateTest.GenerateTestCommand(userId, [blockId], ["open_answer"], 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.TestId);
        _bgJobService.Received(1).EnqueueGenerateTest(
            Arg.Any<Guid>(), userId, Arg.Any<Guid[]>(), Arg.Any<string[]>(), 10);
    }
}
