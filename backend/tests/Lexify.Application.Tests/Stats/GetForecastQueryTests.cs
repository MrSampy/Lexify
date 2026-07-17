using Lexify.Application.Stats.Queries.GetForecast;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Stats;

public class GetForecastQueryTests
{
    private readonly IWordRepository _wordRepo = Substitute.For<IWordRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private GetForecastQueryHandler CreateHandler() => new(_wordRepo);

    [Fact]
    public async Task Handle_BucketsTimesPerDay_AndFillsZeros()
    {
        // Anchor to UTC midnight and keep every offset within its own calendar day,
        // so bucketing (by DayNumber) is independent of the wall-clock time the test runs.
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        IReadOnlyList<DateTimeOffset> times =
            [today.AddHours(6), today.AddDays(2).AddHours(6), today.AddDays(2).AddHours(9)];
        _wordRepo.GetScheduledReviewTimesAsync(_userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(times);

        var result = await CreateHandler().Handle(
            new GetForecastQuery(_userId, Days: 7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var days = result.Value!.Days;
        Assert.Equal(7, days.Count);
        Assert.Equal(2, days[2].Count);
        Assert.Equal(0, days[1].Count);
        Assert.Equal(days[0].Date.AddDays(2), days[2].Date);
    }

    [Fact]
    public async Task Handle_OverdueCollapsesIntoToday()
    {
        // Overdue days and a same-day future time all collapse to day 0; anchor to UTC
        // midnight so the "today" entry can't roll into tomorrow near midnight.
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        IReadOnlyList<DateTimeOffset> times = [today.AddDays(-30), today.AddDays(-1), today.AddHours(12)];
        _wordRepo.GetScheduledReviewTimesAsync(_userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(times);

        var result = await CreateHandler().Handle(
            new GetForecastQuery(_userId), CancellationToken.None);

        Assert.Equal(3, result.Value!.Days[0].Count);
    }

    [Fact]
    public async Task Handle_ClampsHorizon()
    {
        _wordRepo.GetScheduledReviewTimesAsync(_userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateHandler().Handle(
            new GetForecastQuery(_userId, Days: 365), CancellationToken.None);

        Assert.Equal(30, result.Value!.Days.Count);
    }
}
