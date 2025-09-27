using FakeItEasy;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Services;
using Nb.Tolls.Application.Services.Implementations;
using Xunit;

namespace Nb.Tolls.Application.UnitTests.Services;

public class TollTimeServiceTests
{
    private readonly ILogger<TollTimeService> _logger = A.Fake<ILogger<TollTimeService>>();
    private readonly TollTimeService _sut;
    private readonly ITollDateService _tollDateService;

    public TollTimeServiceTests()
    {
        _tollDateService = A.Fake<ITollDateService>();
        _sut = new TollTimeService(_tollDateService, _logger);
    }

    [Fact]
    public async Task GetEligibleTollFeeTimes_Filters_TollFreeDates_And_Times()
    {
        // Arrange
        var sunday = new DateTimeOffset(2025, 9, 28, 12, 0, 0, TimeSpan.Zero);
        var early = new DateTimeOffset(2025, 9, 25, 5, 30, 0, TimeSpan.Zero);
        var late = new DateTimeOffset(2025, 9, 25, 19, 30, 0, TimeSpan.Zero);
        var ok = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);

        var input = new List<DateTime> { sunday.UtcDateTime, early.UtcDateTime, late.UtcDateTime, ok.UtcDateTime };
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(sunday.UtcDateTime))
            .Returns(true);
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(early.UtcDateTime))
            .Returns(true);
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(late.UtcDateTime))
            .Returns(true);
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(ok.UtcDateTime))
            .Returns(false);

        // Act
        var result = await _sut.GetEligibleTollFeeTimes(input);

        // Assert
        Assert.Single(result);
        Assert.Equal(ok, result[0]);
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(sunday.UtcDateTime))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(early.UtcDateTime))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(late.UtcDateTime))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(ok.UtcDateTime))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async void GetEligibleTollFeeTimes_ReturnsEmpty_When_AllAreTollFree()
    {
        var input = new List<DateTimeOffset>
        {
            new(2025, 9, 28, 10, 0, 0, TimeSpan.Zero),
            new(2025, 9, 25, 5, 59, 0, TimeSpan.Zero),
            new(2025, 9, 25, 19, 45, 0, TimeSpan.Zero)
        };
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(A<DateTime>._))
            .Returns(true);

        var result = await _sut.GetEligibleTollFeeTimes(input.Select(d => d.UtcDateTime).ToList());

        Assert.Empty(result);
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(A<DateTime>._))
            .MustHaveHappenedANumberOfTimesMatching(i => i == 3);
    }


    [Fact]
    public void GetNonOverlappingTollTimes_Keeps_First_And_Every_60_Minutes_OrMore()
    {
        var date = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dateTwo = date.AddMinutes(30);
        var dateThree = date.AddMinutes(60);
        var dateFour = date.AddMinutes(130);

        var input = new List<DateTime> { date.UtcDateTime, dateTwo.UtcDateTime, dateThree.UtcDateTime, dateFour.UtcDateTime };

        // Act
        var result = _sut.GetNonOverlappingTollTimes(input);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(date, result[0]);
        Assert.Equal(dateThree, result[1]);
        Assert.Equal(dateFour, result[2]);
    }

    [Fact]
    public void GetNonOverlappingTollTimes_EmptyInput_ReturnsEmpty()
    {
        var result = _sut.GetNonOverlappingTollTimes([]);
        Assert.Empty(result);
    }

    [Fact]
    public void GetOverlappingTollTimes_Returns_Only_Those_Within_60_Minutes_Of_Anchor()
    {
        var firstTollInWindow = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var secondToll = firstTollInWindow.AddMinutes(30);
        var thirdToll = firstTollInWindow.AddMinutes(65);
        var fourthToll = thirdToll.AddMinutes(65);

        var input = new List<DateTime>
        {
            firstTollInWindow.UtcDateTime, secondToll.UtcDateTime, thirdToll.UtcDateTime, fourthToll.UtcDateTime
        };

        // Act
        var result = _sut.GetOverlappingTollTimes(input);

        // Assert
        Assert.True(result.Count == 2);
        Assert.Equal(secondToll, result[1]);
        Assert.Equal(firstTollInWindow, result[0]);
    }

    [Fact]
    public void GetOverlappingTollTimes_Exactly60Minutes_IsNotOverlapping()
    {
        var anchor = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var exactly60 = anchor.AddMinutes(60);

        var result = _sut.GetOverlappingTollTimes([anchor.UtcDateTime, exactly60.UtcDateTime]);

        Assert.Empty(result);
    }

    [Fact]
    public void GetOverlappingTollTimes_EmptyInput_ReturnsEmpty()
    {
        var result = _sut.GetOverlappingTollTimes([]);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(5, 59, true)]
    [InlineData(6, 0, false)]
    [InlineData(18, 29, false)]
    [InlineData(18, 30, false)]
    [InlineData(19, 0, false)]
    [InlineData(19, 30, true)]
    public void IsTollFreeTime_ReturnsExpected(int hour, int minute, bool expected)
    {
        var date = new DateTime(2025, 9, 25, hour, minute, 0);

        var isFree = TollTimeService.IsTollFreeTime(date);

        Assert.Equal(expected, isFree);
    }
}