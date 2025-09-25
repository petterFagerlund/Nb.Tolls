using FakeItEasy;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Clients;
using Nb.Tolls.Application.Services.Implementations;
using Xunit;

namespace Nb.Tolls.Application.UnitTests.Services;

public class TollDateServiceTests
{
    private readonly INagerHttpClient _nager = A.Fake<INagerHttpClient>();
    private readonly ILogger<TollDateService> _logger = A.Fake<ILogger<TollDateService>>();
    private readonly TollDateService _sut;

    public TollDateServiceTests()
    {
        _sut = new TollDateService(_logger, _nager);
    }

    [Fact]
    public async Task IsTollFreeDateAsync_July_ReturnsTrue_And_DoesNotCallClient()
    {
        // Arrange
        var ts = new DateTimeOffset(2025, 7, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        var result = await _sut.IsTollFreeDateAsync(ts);

        // Assert
        Assert.True(result);
        A.CallTo(() => _nager.IsPublicHolidayAsync(A<DateOnly>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_Saturday_ReturnsTrue_And_DoesNotCallClient()
    {
        var date = new DateTimeOffset(2025, 9, 27, 10, 0, 0, TimeSpan.Zero);

        // Act
        var result = await _sut.IsTollFreeDateAsync(date);

        // Assert
        Assert.True(result);
        A.CallTo(() => _nager.IsPublicHolidayAsync(A<DateOnly>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_Sunday_ReturnsTrue_And_DoesNotCallClient()
    {
        // Arrange
        var date = new DateTimeOffset(2025, 9, 28, 10, 0, 0, TimeSpan.Zero);

        // Act
        var result = await _sut.IsTollFreeDateAsync(date);

        // Assert
        Assert.True(result);
        A.CallTo(() => _nager.IsPublicHolidayAsync(A<DateOnly>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_DayBeforeHoliday_ReturnsTrue_And_ChecksBothDays()
    {
        var date = new DateTimeOffset(2025, 12, 24, 10, 0, 0, TimeSpan.Zero);

        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 12, 24), A<CancellationToken>._))
            .Returns(Task.FromResult(false));
        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 12, 25), A<CancellationToken>._))
            .Returns(Task.FromResult(true));

        // Act
        var result = await _sut.IsTollFreeDateAsync(date);

        // Assert
        Assert.True(result);
        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 12, 24), A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 12, 25), A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_WeekdayNotHoliday_ReturnsFalse_And_CallsClientTwice()
    {
        // Arrange
        var date = new DateTimeOffset(2025, 9, 25, 10, 0, 0, TimeSpan.Zero);

        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 9, 25), A<CancellationToken>._))
            .Returns(Task.FromResult(false));
        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 9, 26), A<CancellationToken>._))
            .Returns(Task.FromResult(false));

        // Act
        var result = await _sut.IsTollFreeDateAsync(date);

        // Assert
        Assert.False(result);
        A.CallTo(() => _nager.IsPublicHolidayAsync(A<DateOnly>._, A<CancellationToken>._)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task IsTollFreeDateAsync_NagerClientThrows_ReturnsFalse_And_DoesNotThrow()
    {
        var date = new DateTimeOffset(2025, 9, 25, 10, 0, 0, TimeSpan.Zero);

        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 9, 25), A<CancellationToken>._))
            .Throws(new Exception("network failure"));
        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 9, 26), A<CancellationToken>._))
            .Throws(new Exception("network failure"));

        var result = await _sut.IsTollFreeDateAsync(date);

        // Assert
        Assert.False(result);
        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 9, 25), A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _nager.IsPublicHolidayAsync(new DateOnly(2025, 9, 26), A<CancellationToken>._)).MustHaveHappened();
    }
}