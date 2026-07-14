using Lexify.Application.AI.Services;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.AI;

public class AiQuotaServiceTests
{
    private readonly ISystemSettingRepository _settings = Substitute.For<ISystemSettingRepository>();
    private readonly IAiCallLogRepository _logs = Substitute.For<IAiCallLogRepository>();

    private AiQuotaService CreateService() => new(_settings, _logs);

    private void GivenLimit(string value) =>
        _settings.GetByKeyAsync(SystemSetting.Keys.MaxAiCallsPerUserPerDay, Arg.Any<CancellationToken>())
            .Returns(new SystemSetting(SystemSetting.Keys.MaxAiCallsPerUserPerDay, value, "int"));

    private void GivenUsedToday(int count) =>
        _logs.CountByUserSinceAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(count);

    [Fact]
    public async Task UnderLimit_IsNotExceeded()
    {
        GivenLimit("50");
        GivenUsedToday(49);

        var result = await CreateService().CheckAsync(Guid.NewGuid());

        Assert.False(result.IsExceeded);
        Assert.Equal(50, result.Limit);
        Assert.Equal(49, result.Used);
    }

    [Fact]
    public async Task AtLimit_IsExceeded()
    {
        // The limit is a ceiling, not an allowance for one more call.
        GivenLimit("50");
        GivenUsedToday(50);

        var result = await CreateService().CheckAsync(Guid.NewGuid());

        Assert.True(result.IsExceeded);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task ZeroOrNegativeLimit_MeansUnlimited(string limit)
    {
        GivenLimit(limit);
        GivenUsedToday(10_000);

        var result = await CreateService().CheckAsync(Guid.NewGuid());

        Assert.False(result.IsExceeded);
        // Unlimited must not even count — no reason to scan the log table.
        await _logs.DidNotReceive().CountByUserSinceAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingSettingRow_FallsBackToDefaultLimitRatherThanUnlimited()
    {
        _settings.GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SystemSetting?)null);
        GivenUsedToday(50);

        var result = await CreateService().CheckAsync(Guid.NewGuid());

        // A missing row must fail closed — otherwise deleting a setting hands out unlimited spend.
        Assert.True(result.IsExceeded);
        Assert.Equal(50, result.Limit);
    }

    [Fact]
    public async Task UnparseableSetting_FallsBackToDefaultLimit()
    {
        GivenLimit("not-a-number");
        GivenUsedToday(50);

        var result = await CreateService().CheckAsync(Guid.NewGuid());

        Assert.True(result.IsExceeded);
        Assert.Equal(50, result.Limit);
    }

    [Fact]
    public async Task CountsFromMidnightUtc_WithZeroOffset()
    {
        GivenLimit("50");
        GivenUsedToday(1);

        var userId = Guid.NewGuid();
        await CreateService().CheckAsync(userId);

        // The window is the UTC calendar-day boundary (so the quota resets at midnight rather than
        // trailing 24h behind the last call), and the offset must be zero — a non-zero offset is
        // rejected outright by Npgsql when compared against a timestamptz column.
        await _logs.Received(1).CountByUserSinceAsync(
            userId,
            Arg.Is<DateTimeOffset>(d =>
                d.Offset == TimeSpan.Zero &&
                d.TimeOfDay == TimeSpan.Zero &&
                d.Date == DateTime.UtcNow.Date),
            Arg.Any<CancellationToken>());
    }
}
