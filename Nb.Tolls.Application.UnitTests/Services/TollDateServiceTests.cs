using FakeItEasy;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.ApiClients;
using Nb.Tolls.Application.Services.Implementations;
using Xunit;

namespace Nb.Tolls.Application.UnitTests.Services;

public class TollDateServiceTests
{
    private readonly IPublicHolidayApiClient _publicHolidayApiClient = A.Fake<IPublicHolidayApiClient>();
    private readonly ILogger<TollDateService> _logger = A.Fake<ILogger<TollDateService>>();
    private readonly TollDateService _sut;

    public TollDateServiceTests()
    {
        _sut = new TollDateService(_logger, _publicHolidayApiClient);
    }

    [Fact]
    public async Task IsTollFreeDateAsync_July_ReturnsTrue_And_DoesNotCallClient()
    {
        // Arrange
        var date = new DateTime(2025, 7, 15, 12, 0, 0);

        // Act
        var result = await _sut.IsTollFreeDate(date);

        // Assert
        Assert.True(result);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(A<DateOnly>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_Saturday_ReturnsTrue_And_DoesNotCallClient()
    {
        var date = new DateTime(2025, 9, 27, 10, 0, 0);

        // Act
        var result = await _sut.IsTollFreeDate(date);

        // Assert
        Assert.True(result);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(A<DateOnly>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_Sunday_ReturnsTrue_And_DoesNotCallClient()
    {
        // Arrange
        var date = new DateTime(2025, 9, 28, 10, 0, 0);

        // Act
        var result = await _sut.IsTollFreeDate(date);

        // Assert
        Assert.True(result);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(A<DateOnly>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_DayBeforeHoliday_ReturnsTrue_And_ChecksBothDays()
    {
        var date = new DateTime(2025, 12, 24, 10, 0, 0);

        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 12, 24), A<CancellationToken>._))
            .Returns(Task.FromResult(false));
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 12, 25), A<CancellationToken>._))
            .Returns(Task.FromResult(true));

        // Act
        var result = await _sut.IsTollFreeDate(date);

        // Assert
        Assert.True(result);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 12, 24), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 12, 25), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_WeekdayNotHoliday_ReturnsFalse_And_CallsClientTwice()
    {
        // Arrange
        var date = new DateTime(2025, 9, 25, 10, 0, 0);

        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 9, 25), A<CancellationToken>._))
            .Returns(Task.FromResult(false));
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 9, 26), A<CancellationToken>._))
            .Returns(Task.FromResult(false));

        // Act
        var result = await _sut.IsTollFreeDate(date);

        // Assert
        Assert.False(result);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(A<DateOnly>._, A<CancellationToken>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_NagerClientThrows_ReturnsFalse_And_DoesNotThrow()
    {
        var date = new DateTime(2025, 9, 25, 10, 0, 0);

        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 9, 25), A<CancellationToken>._))
            .Throws(new Exception("network failure"));
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 9, 26), A<CancellationToken>._))
            .Throws(new Exception("network failure"));

        var result = await _sut.IsTollFreeDate(date);

        // Assert
        Assert.False(result);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 9, 25), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(new DateOnly(2025, 9, 26), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeCalendarDateAsync_ShouldReturnTrue_ForJuly()
    {
        // Arrange
        var date = new DateOnly(2025, 7, 15);

        // Act
        var result = await _sut.IsTollFreeCalendarDate(date);

        // Act
        Assert.True(result);
    }

    [Fact]
    public async Task IsTollFreeCalendarDateAsync_ShouldReturnTrue_ForSaturday()
    {
        // Arrange
        var date = new DateOnly(2025, 9, 27);

        // Act
        var result = await _sut.IsTollFreeCalendarDate(date);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTollFreeCalendarDateAsync_ShouldReturnTrue_ForPublicHoliday()
    {
        //Arrange
        var date = new DateOnly(2025, 12, 25);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(date, A<CancellationToken>._)).Returns(Task.FromResult(true));

        //Act
        var result = await _sut.IsTollFreeCalendarDate(date);

        //Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTollFreeCalendarDateAsync_ShouldReturnTrue_IfNextDayIsPublicHoliday()
    {
        //Arrange
        var date = new DateOnly(2025, 12, 24);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(date, A<CancellationToken>._)).Returns(Task.FromResult(false));
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(date.AddDays(1), A<CancellationToken>._))
            .Returns(Task.FromResult(true));

        //Act
        var result = await _sut.IsTollFreeCalendarDate(date);

        //Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTollFreeCalendarDateAsync_ShouldReturnFalse_WhenNoConditionsMet()
    {
        //Arrange
        var date = new DateOnly(2025, 9, 24);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(date, A<CancellationToken>._)).Returns(Task.FromResult(false));
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(date.AddDays(1), A<CancellationToken>._))
            .Returns(Task.FromResult(false));

        //Act
        var result = await _sut.IsTollFreeCalendarDate(date);

        //Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsPublicHolidayOrSundayAsync_ShouldReturnTrue_ForSunday()
    {
        //Arrange
        var date = new DateOnly(2025, 9, 28);

        //Act
        var result = await _sut.IsPublicHolidayOrSundayAsync(date);

        //Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsPublicHolidayOrSundayAsync_ShouldReturnTrue_ForPublicHoliday()
    {
        //Arrange
        var date = new DateOnly(2025, 12, 25);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(date, A<CancellationToken>._)).Returns(Task.FromResult(true));

        //Act
        var result = await _sut.IsPublicHolidayOrSundayAsync(date);

        //Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsPublicHolidayOrSundayAsync_ShouldReturnFalse_ForNormalDay()
    {
        //Arrange
        var date = new DateOnly(2025, 9, 24);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(date, A<CancellationToken>._)).Returns(Task.FromResult(false));

        //Act
        var result = await _sut.IsPublicHolidayOrSundayAsync(date);

        //Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsPublicHolidayOrSundayAsync_ShouldReturnFalse_OnException_AndLogWarning()
    {
        //Arrange
        var date = new DateOnly(2025, 9, 24);
        A.CallTo(() => _publicHolidayApiClient.IsPublicHolidayAsync(date, A<CancellationToken>._)).Throws(new Exception("API error"));

        //Act
        var result = await _sut.IsPublicHolidayOrSundayAsync(date);

        //Assert
        Assert.False(result);
    }
}