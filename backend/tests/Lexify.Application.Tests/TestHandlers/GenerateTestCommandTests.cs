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
    private readonly ISystemSettingRepository _settingRepo;
    private readonly IBackgroundJobService _bgJobService;
    private readonly IAiQuotaService _aiQuota;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppGenerateTest.GenerateTestCommandHandler _handler;

    public GenerateTestCommandTests()
    {
        _blockRepo = Substitute.For<IWordBlockRepository>();
        _wordRepo = Substitute.For<IWordRepository>();
        _testRepo = Substitute.For<ITestRepository>();
        _settingRepo = Substitute.For<ISystemSettingRepository>();
        _bgJobService = Substitute.For<IBackgroundJobService>();
        _aiQuota = Substitute.For<IAiQuotaService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        // Default: quota never blocks, so the existing cases exercise their own logic.
        _aiQuota.CheckAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(AiQuotaCheck.Unlimited);

        _handler = new AppGenerateTest.GenerateTestCommandHandler(
            _blockRepo, _wordRepo, _testRepo, _settingRepo, _bgJobService, _aiQuota, _unitOfWork);
    }

    [Fact]
    public async Task Handle_DailyAiQuotaExceeded_ReturnsFailureAndDoesNotCreateTest()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        _aiQuota.CheckAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AiQuotaCheck(IsExceeded: true, Limit: 50, Used: 50));

        var command = new AppGenerateTest.GenerateTestCommand(userId, [blockId], ["open_answer"], 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
        Assert.Contains("Daily AI limit", result.ErrorMessage);

        // The cap is checked before anything is persisted or queued — otherwise a test row would be
        // stranded in "generating" forever.
        await _testRepo.DidNotReceive().AddAsync(Arg.Any<Test>(), Arg.Any<CancellationToken>());
        _bgJobService.DidNotReceive().EnqueueGenerateTest(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid[]>(), Arg.Any<string[]>(), Arg.Any<int>());
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
