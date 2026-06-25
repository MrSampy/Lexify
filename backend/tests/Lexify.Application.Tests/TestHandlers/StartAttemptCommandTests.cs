using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;
using AppStartAttempt = Lexify.Application.Tests.Commands.StartAttempt;

namespace Lexify.Application.Tests.TestHandlers;

public class StartAttemptCommandTests
{
    private readonly ITestRepository _testRepo;
    private readonly ITestAttemptRepository _attemptRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppStartAttempt.StartAttemptCommandHandler _handler;

    public StartAttemptCommandTests()
    {
        _testRepo = Substitute.For<ITestRepository>();
        _attemptRepo = Substitute.For<ITestAttemptRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AppStartAttempt.StartAttemptCommandHandler(_testRepo, _attemptRepo, _unitOfWork);
    }

    [Fact]
    public async Task Handle_TestNotFound_ReturnsNotFound()
    {
        _testRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Test?)null);

        var result = await _handler.Handle(
            new AppStartAttempt.StartAttemptCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ForeignTest_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var test = new Test(ownerId, "Quiz");
        test.MarkReady(5);

        _testRepo.GetByIdAsync(test.Id, Arg.Any<CancellationToken>()).Returns(test);

        var result = await _handler.Handle(
            new AppStartAttempt.StartAttemptCommand(test.Id, Guid.NewGuid()), // different userId
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_TestStillGenerating_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var test = new Test(userId, "Quiz"); // Status = Generating

        _testRepo.GetByIdAsync(test.Id, Arg.Any<CancellationToken>()).Returns(test);

        var result = await _handler.Handle(
            new AppStartAttempt.StartAttemptCommand(test.Id, userId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
        Assert.Contains(Test.Statuses.Generating, result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ArchivedTest_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var test = new Test(userId, "Quiz");
        test.Archive();

        _testRepo.GetByIdAsync(test.Id, Arg.Any<CancellationToken>()).Returns(test);

        var result = await _handler.Handle(
            new AppStartAttempt.StartAttemptCommand(test.Id, userId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    [Fact]
    public async Task Handle_ReadyTest_ReturnsAttemptId()
    {
        var userId = Guid.NewGuid();
        var test = new Test(userId, "Quiz");
        test.MarkReady(10);

        _testRepo.GetByIdAsync(test.Id, Arg.Any<CancellationToken>()).Returns(test);

        var result = await _handler.Handle(
            new AppStartAttempt.StartAttemptCommand(test.Id, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.AttemptId);
    }
}
