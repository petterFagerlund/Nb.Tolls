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
    public async Task ExtractEligibleTollFeeTimes_Filters_TollFreeDates_And_Times()
    {
        // Arrange
        var sunday = new DateTimeOffset(2025, 9, 28, 12, 0, 0, TimeSpan.Zero);
        var early = new DateTimeOffset(2025, 9, 25, 5, 30, 0, TimeSpan.Zero);
        var late = new DateTimeOffset(2025, 9, 25, 19, 30, 0, TimeSpan.Zero);
        var ok = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);

        var input = new List<DateTimeOffset> { sunday, early, late, ok };
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(sunday))
            .Returns(true);
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(early))
            .Returns(true);
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(late))
            .Returns(true);
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(ok))
            .Returns(false);
        
        // Act
        var result = await _sut.ExtractEligibleTollFeeTimes(input);

        // Assert
        Assert.Single(result);
        Assert.Equal(ok, result[0]);
    }

    [Fact]
    public async void ExtractEligibleTollFeeTimes_ReturnsEmpty_When_AllAreTollFree()
    {
        var input = new List<DateTimeOffset>
        {
            new(2025, 9, 28, 10, 0, 0, TimeSpan.Zero),
            new(2025, 9, 25, 5, 59, 0, TimeSpan.Zero),
            new(2025, 9, 25, 19, 45, 0, TimeSpan.Zero)
        };
        A.CallTo(() => _tollDateService.IsTollFreeDateAsync(A<DateTimeOffset>._))
            .Returns(true);

        var result = await _sut.ExtractEligibleTollFeeTimes(input);

        Assert.Empty(result);
    }


    [Fact]
    public void ExtractNonOverlappingTollTimes_Keeps_First_And_Every_60_Minutes_OrMore()
    {
        var date = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dateTwo = date.AddMinutes(30);
        var dateThree = date.AddMinutes(60);
        var dateFour = date.AddMinutes(130);

        var input = new List<DateTimeOffset> { date, dateTwo, dateThree, dateFour };

        // Act
        var result = _sut.ExtractNonOverlappingTollTimes(input);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(date, result[0]);
        Assert.Equal(dateThree, result[1]);
        Assert.Equal(dateFour, result[2]);
    }

    [Fact]
    public void ExtractNonOverlappingTollTimes_EmptyInput_ReturnsEmpty()
    {
        var result = _sut.ExtractNonOverlappingTollTimes([]);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractOverlappingTollTimes_Returns_Only_Those_Within_60_Minutes_Of_Anchor()
    {
        var anchor = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var within = anchor.AddMinutes(30);
        var outside1 = anchor.AddMinutes(65);
        var outside2 = outside1.AddMinutes(65);

        var input = new List<DateTimeOffset> { anchor, within, outside1, outside2 };

        // Act
        var result = _sut.ExtractOverlappingTollTimes(input);

        // Assert
        Assert.Single(result);
        Assert.Equal(within, result[0]);
    }

    [Fact]
    public void ExtractOverlappingTollTimes_Exactly60Minutes_IsNotOverlapping()
    {
        var anchor = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var exactly60 = anchor.AddMinutes(60);

        var result = _sut.ExtractOverlappingTollTimes([anchor, exactly60]);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractOverlappingTollTimes_EmptyInput_ReturnsEmpty()
    {
        var result = _sut.ExtractOverlappingTollTimes([]);
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
        var date = new DateTimeOffset(2025, 9, 25, hour, minute, 0, TimeSpan.Zero);

        var isFree = _sut.IsTollFreeTime(date);

        Assert.Equal(expected, isFree);
    }
}