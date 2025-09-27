using FakeItEasy;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Application.Services.Implementations;
using Nb.Tolls.Domain.Results;
using Xunit;

namespace Nb.Tolls.Application.UnitTests.Services;

public class TollFeesCalculationServiceTests
{
    private readonly ITollFeeRepository _tollFeeRepository;
    private readonly TollFeesCalculationService _sut;

    public TollFeesCalculationServiceTests()
    {
        var logger = A.Fake<ILogger<TollFeesCalculationService>>();
        _tollFeeRepository = A.Fake<ITollFeeRepository>();
        _sut = new TollFeesCalculationService(_tollFeeRepository, logger);
    }

    [Fact]
    public void CalculateDailyTollFeeTotals_Should_ReturnEmpty_WhenInputIsEmpty()
    {
        // Arrange
        var tollFees = new List<TollTimeFeeResult>();

        // Act
        var result = _sut.CalculateDailyTollFeeTotals(tollFees);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.TollFees);
    }

    [Fact]
    public void CalculateDailyTollFeeTotals_Should_SumTollsPerDay_AndCapAt60()
    {
        // Arrange
        var tollFees = new List<TollTimeFeeResult>
        {
            new() { TollTime = new DateTime(2025, 09, 29, 06, 00, 0), TollFee = 30m },
            new() { TollTime = new DateTime(2025, 09, 29, 07, 00, 0), TollFee = 40m }, // total would be 70 -> capped at 60
            new() { TollTime = new DateTime(2025, 09, 30, 08, 00, 0), TollFee = 25m }
        };

        // Act
        var result = _sut.CalculateDailyTollFeeTotals(tollFees);

        // Assert
        Assert.Equal(2, result.TollFees.Count);

        var firstDay = result.TollFees.First(d => d.Date == DateOnly.FromDateTime(new DateTime(2025, 09, 29)));
        var secondDay = result.TollFees.First(d => d.Date == DateOnly.FromDateTime(new DateTime(2025, 09, 30)));

        Assert.Equal(60m, firstDay.TollFee);
        Assert.Equal(25m, secondDay.TollFee);
    }

    [Fact]
    public void CalculateDailyTollFeeTotals_Should_SkipZeroTolls()
    {
        // Arrange
        var tollFees = new List<TollTimeFeeResult>
        {
            new() { TollTime = new DateTime(2025, 09, 29, 06, 00, 0), TollFee = 0m },
            new() { TollTime = new DateTime(2025, 09, 29, 07, 00, 0), TollFee = 20m }
        };

        // Act
        var result = _sut.CalculateDailyTollFeeTotals(tollFees);

        // Assert
        Assert.Single(result.TollFees);
        Assert.Equal(20m, result.TollFees[0].TollFee);
    }

    [Fact]
    public void CalculateDailyTollFeeTotals_Should_OrderResultsByDate()
    {
        // Arrange
        var tollFees = new List<TollTimeFeeResult>
        {
            new() { TollTime = new DateTime(2025, 10, 01, 08, 00, 0), TollFee = 20m },
            new() { TollTime = new DateTime(2025, 09, 30, 08, 00, 0), TollFee = 20m }
        };

        // Act
        var result = _sut.CalculateDailyTollFeeTotals(tollFees);

        // Assert
        Assert.Equal(DateOnly.FromDateTime(new DateTime(2025, 09, 30)), result.TollFees[0].Date);
        Assert.Equal(DateOnly.FromDateTime(new DateTime(2025, 10, 01)), result.TollFees[1].Date);
    }


    [Fact]
    public void CalculateNonOverlappingTollFees_AllZeroFees_ReturnsNotFound()
    {
        var date = new DateTime(2025, 9, 25, 6, 0, 0);
        var dateTwo = new DateTime(2025, 9, 25, 7, 0, 0);
        var input = new[] { date, dateTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 0m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 0m }));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);

        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_PartialZeroFees_SumsNonZeroAndCaps()
    {
        var date = new DateTime(2025, 9, 25, 6, 0, 0);
        var input = new[] { date, date.AddMinutes(15), date.AddHours(1) };

        A.CallTo(() => _tollFeeRepository.GetTollFee(input[0]))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 0m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[1]))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 25m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[2]))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 45m }));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(60m, day.TollFee);

        A.CallTo(() => _tollFeeRepository.GetTollFee(input[0]))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[1]))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[2]))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_EmptyInput_ReturnsUnSuccessful()
    {
        // Arrange
        var input = Array.Empty<DateTime>();

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_FetchFeesFailure_ReturnsUnSuccessful()
    {
        // Arrange
        var date = new DateTime(2025, 9, 25, 7, 0, 0);
        var dateTwo = new DateTime(2025, 9, 25, 8, 0, 0);
        var input = new[] { date, dateTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .Returns(ApplicationResult.WithError<TollFeeResult>("boom"));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo)).MustNotHaveHappened();
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_HappyPath_SumsPerDay_WithCap60()
    {
        // Arrange (same day)
        var date = new DateTime(2025, 9, 25, 6, 0, 0);
        var input = new[] { date, date.AddMinutes(30), date.AddHours(2) };

        A.CallTo(() => _tollFeeRepository.GetTollFee(input[0]))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[1]))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 25m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[2]))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 30m }));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(new DateOnly(2025, 9, 25), day.Date);
        Assert.Equal(60m, day.TollFee);
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[0]))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[1]))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[2]))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_MultipleDays_ReturnsSeparateEntries()
    {
        var date = new DateTime(2025, 9, 25, 7, 0, 0);
        var dateWithDifferentTime = new DateTime(2025, 9, 25, 8, 0, 0);
        var dateThree = new DateTime(2025, 9, 26, 9, 0, 0);

        var input = new[] { date, dateWithDifferentTime, dateThree };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateWithDifferentTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateThree))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 40m }));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Result.TollFees.Count);

        var day1 = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 25));
        var day2 = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 26));

        Assert.Equal(25m, day1.TollFee);
        Assert.Equal(40m, day2.TollFee);
        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateWithDifferentTime))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateThree))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateOverlappingTollFees_EmptyInput_ReturnsUnSuccessful()
    {
        //Arrange
        var input = Array.Empty<DateTime>();

        //Act
        var result = _sut.CalculateOverlappingTollFees(input);

        //Assert
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void CalculateOverlappingTollFees_SingleDay_ReturnsMaxFee()
    {
        //Arrange
        var baseDate = new DateTime(2025, 9, 25, 8, 0, 0);
        var times = new[] { baseDate, baseDate.AddMinutes(30), baseDate.AddMinutes(45) };

        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate.AddMinutes(30)))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate.AddMinutes(45)))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));

        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(45m, day.TollFee);
        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate.AddMinutes(30)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate.AddMinutes(45)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateOverlappingTollFees_MultipleTimeWindowsSameDay_ReturnsCorrectFee()
    {
        //Arrange
        var date = new DateTime(2025, 9, 25, 8, 0, 0);
        var times = new[] { date, date.AddMinutes(40), date.AddMinutes(70) };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(date.AddMinutes(40)))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 25m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(date.AddMinutes(70)))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));

        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(50m, day.TollFee);
        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(date.AddMinutes(40)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(date.AddMinutes(70)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateOverlappingTollFees_DayCapIs_60()
    {
        //Arrange
        var date = new DateTime(2025, 9, 25, 6, 0, 0);
        var times = new[]
        {
            date, date.AddMinutes(30), date.AddMinutes(70), date.AddMinutes(90), date.AddMinutes(140), date.AddMinutes(160),
            date.AddMinutes(210), date.AddMinutes(230)
        };

        foreach (var time in times)
        {
            var fee = time.Minute % 60 == 30 || time.Minute % 60 == 40 ? 20m : 10m;
            A.CallTo(() => _tollFeeRepository.GetTollFee(time))
                .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = fee }));
        }

        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(60m, day.TollFee);
        A.CallTo(() => _tollFeeRepository.GetTollFee(A<DateTime>._))
            .MustHaveHappenedANumberOfTimesMatching(x => x == times.Length);
    }

    [Fact]
    public void CalculateOverlappingTollFees_GroupsByDay_ReturnsSeparateEntries()
    {
        //Arrange
        var dayOne = new DateTime(2025, 9, 25, 8, 0, 0);
        var dayTwo = new DateTime(2025, 9, 26, 7, 0, 0);
        var times = new[] { dayOne, dayOne.AddMinutes(30), dayTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(dayOne))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dayOne.AddMinutes(30)))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dayTwo))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));

        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Result.TollFees.Count);

        var dayOneResult = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 25));
        var dayTwoResult = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 26));

        Assert.Equal(30m, dayOneResult.TollFee);
        Assert.Equal(15m, dayTwoResult.TollFee);
        A.CallTo(() => _tollFeeRepository.GetTollFee(dayOne))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(dayOne.AddMinutes(30)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeeRepository.GetTollFee(dayTwo))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateOverlappingTollFees_FetchFeesFailure_ReturnsUnSuccessful()
    {
        //Arrange
        var date = new DateTime(2025, 9, 25, 8, 0, 0);
        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .Returns(ApplicationResult.WithError<TollFeeResult>("boom"));

        //Act
        var result = _sut.CalculateOverlappingTollFees(new[] { date });

        //Assert
        Assert.False(result.IsSuccessful);
        A.CallTo(() => _tollFeeRepository.GetTollFee(date))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GetTollFees_ShouldReturnError_WhenNoTollTimesProvided()
    {
        // Arrange
        var emptyList = new List<DateTime>();

        // Act
        var result = _sut.GetTollFees(emptyList);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("No toll times provided", result.Messages.First());
    }

    [Fact]
    public void GetTollFees_ShouldReturnError_WhenRepositoryReturnsFailure()
    {
        // Arrange
        var tollTime = DateTime.UtcNow;
        A.CallTo(() => _tollFeeRepository.GetTollFee(tollTime))
            .Returns(ApplicationResult.WithError<TollFeeResult>("Repository failure"));

        var list = new List<DateTime> { tollTime };

        // Act
        var result = _sut.GetTollFees(list);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("Internal error occurred", result.Messages.First());
    }

    [Fact]
    public void GetTollFees_ShouldReturnTollFees_WhenRepositoryReturnsSuccess()
    {
        // Arrange
        var tollTime1 = DateTime.UtcNow;
        var tollTime2 = tollTime1.AddMinutes(30);

        A.CallTo(() => _tollFeeRepository.GetTollFee(tollTime1))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));

        A.CallTo(() => _tollFeeRepository.GetTollFee(tollTime2))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));

        var list = new List<DateTime> { tollTime1, tollTime2 };

        // Act
        var result = _sut.GetTollFees(list);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Result?.Count);
        Assert.Equal(10m, result.Result?[0].TollFee);
        Assert.Equal(20m, result.Result?[1].TollFee);
    }
}