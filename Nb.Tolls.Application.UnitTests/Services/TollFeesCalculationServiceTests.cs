using FakeItEasy;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Application.Services.Implementations;
using Nb.Tolls.Domain.Results;
using Xunit;

namespace Nb.Tolls.Application.UnitTests.Services;

public class TollFeesCalculationServiceTests
{
    private readonly ITollFeesRepository _tollFeesRepository;
    private readonly TollFeesCalculationService _sut;

    public TollFeesCalculationServiceTests()
    {
        var logger = A.Fake<ILogger<TollFeesCalculationService>>();
        _tollFeesRepository = A.Fake<ITollFeesRepository>();
        _sut = new TollFeesCalculationService(_tollFeesRepository, logger);
    }

    [Fact]
    public void CalculateTollFees_Should_ReturnEmpty_WhenInputIsEmpty()
    {
        // Arrange
        var tollFees = new List<TollFeeResult>();

        // Act
        var result = _sut.CalculateTollFees(tollFees);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.TollFees);
    }

    [Fact]
    public void CalculateTollFees_Should_SumTollsPerDay_AndCapAt60()
    {
        // Arrange
        var tollFees = new List<TollFeeResult>
        {
            new() { TollTime = new DateTime(2025, 09, 29, 8, 15, 0), TollFee = 30m },
            new() { TollTime = new DateTime(2025, 09, 29, 12, 15, 0), TollFee = 40m },
            new() { TollTime = new DateTime(2025, 09, 30), TollFee = 25m }
        };

        // Act
        var result = _sut.CalculateTollFees(tollFees);

        // Assert
        Assert.Equal(2, result.TollFees.Count);

        var firstDay = result.TollFees.First(d => d.Date == DateOnly.FromDateTime(new DateTime(2025, 09, 29)));
        var secondDay = result.TollFees.First(d => d.Date == DateOnly.FromDateTime(new DateTime(2025, 09, 30)));

        Assert.Equal(60m, firstDay.TollFee);
        Assert.Equal(25m, secondDay.TollFee);
    }

    [Fact]
    public void CalculateTollFees_Should_SkipZeroTolls()
    {
        // Arrange
        var tollFees = new List<TollFeeResult>
        {
            new() { TollTime = new DateTime(2025, 09, 29), TollFee = 0m },
            new() { TollTime = new DateTime(2025, 09, 29), TollFee = 20m }
        };

        // Act
        var result = _sut.CalculateTollFees(tollFees);

        // Assert
        Assert.Single(result.TollFees);
        Assert.Equal(20m, result.TollFees[0].TollFee);
    }

    [Fact]
    public void CalculateTollFees_Should_OrderResultsByDate()
    {
        // Arrange
        var tollFees = new List<TollFeeResult>
        {
            new() { TollTime = new DateTime(2025, 10, 01), TollFee = 20m },
            new() { TollTime = new DateTime(2025, 09, 30), TollFee = 20m }
        };

        // Act
        var result = _sut.CalculateTollFees(tollFees);

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

        var repositoryResponse = new ApplicationResult<List<TollFeeResult>>
        {
            Result = new List<TollFeeResult> { new() { TollFee = 0m } }
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(input))
            .Returns(repositoryResponse);

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);

        A.CallTo(() => _tollFeesRepository.GetTollFees(input))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_PartialZeroFees_SumsNonZeroAndCaps()
    {
        var date = new DateTimeOffset(2025, 9, 25, 6, 0, 0, TimeSpan.Zero);
        var input = new[] { date, date.AddMinutes(15), date.AddHours(2) };
        var ordered = input
            .Select(dateTime => dateTime.UtcDateTime)
            .ToList();
        var repositoryResponse = new List<TollFeeResult>
        {
            new() { TollTime = input[0].UtcDateTime, TollFee = 20m },
            new() { TollTime = input[1].UtcDateTime, TollFee = 35m },
            new() { TollTime = input[2].UtcDateTime, TollFee = 30m },
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
            .Returns(ApplicationResult.WithSuccess(repositoryResponse));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(ordered);

        // Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(60m, day.TollFee);

        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
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
        A.CallTo(() => _tollFeesRepository.GetTollFees(input))
            .Returns(ApplicationResult.WithError<List<TollFeeResult>>("boom"));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
        A.CallTo(() => _tollFeesRepository.GetTollFees(input))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_HappyPath_SumsPerDay_WithCap60()
    {
        // Arrange
        var date = new DateTimeOffset(2025, 9, 25, 6, 0, 0, TimeSpan.Zero);
        var input = new[] { date, date.AddMinutes(61), date.AddMinutes(200), date.AddHours(2) };
        var ordered = input
            .Select(dateTime => dateTime.UtcDateTime)
            .ToList();
        var repositoryResponse = new List<TollFeeResult>
        {
            new() { TollTime = ordered[0], TollFee = 20m },
            new() { TollTime = ordered[1], TollFee = 35m },
            new() { TollTime = ordered[2], TollFee = 30m },
            new() { TollTime = ordered[3], TollFee = 30m },
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(A<List<DateTime>>._))
            .Returns(ApplicationResult.WithSuccess(repositoryResponse));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(ordered);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(DateOnly.FromDateTime(new DateTime(2025,09,25)), day.Date);
        Assert.Equal(60m, day.TollFee);
        A.CallTo(() => _tollFeesRepository.GetTollFees(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_MultipleDays_ReturnsSeparateEntries()
    {
        var date = new DateTimeOffset(2025, 9, 22, 7, 0, 0, TimeSpan.Zero);
        var dateWithDifferentTime = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dateThree = new DateTimeOffset(2025, 9, 26, 9, 0, 0, TimeSpan.Zero);

        var input = new[] { date, dateWithDifferentTime, dateThree };

        var ordered = input
            .Select(dateTime => dateTime.UtcDateTime)
            .ToList();
        var repositoryResponse = new List<TollFeeResult>
        {
            new() { TollTime = input[0].UtcDateTime, TollFee = 20m },
            new() { TollTime = input[1].UtcDateTime, TollFee = 25m },
            new() { TollTime = input[2].UtcDateTime, TollFee = 40m },
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
            .Returns(ApplicationResult.WithSuccess(repositoryResponse));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(ordered);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(3, result.Result.TollFees.Count);

        var day1 = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 25));
        var day2 = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 26));

        Assert.Equal(25m, day1.TollFee);
        Assert.Equal(40m, day2.TollFee);
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
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
        var repositoryResponse = new ApplicationResult<List<TollFeeResult>>
        {
            Result = new List<TollFeeResult> { new() { TollFee = 10m }, new() { TollFee = 20m }, new() { TollFee = 15m } }
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(times))
            .Returns(repositoryResponse);


        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(20m, day.TollFee);
        A.CallTo(() => _tollFeesRepository.GetTollFees(times))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateOverlappingTollFees_MultipleTimeWindowsSameDay_ReturnsCorrectFee()
    {
        //Arrange
        var date = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var times = new[] { date, date.AddMinutes(40), date.AddMinutes(70) };
        var ordered = times
            .Select(dateTime => dateTime.UtcDateTime)
            .ToList();
        var repositoryResponse = new List<TollFeeResult>
        {
            new() { TollTime = times[0].UtcDateTime, TollFee = 20m },
            new() { TollTime = times[1].UtcDateTime, TollFee = 10m },
            new() { TollTime = times[2].UtcDateTime, TollFee = 20m },
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
            .Returns(ApplicationResult.WithSuccess(repositoryResponse));

        //Act
        var result = _sut.CalculateOverlappingTollFees(ordered);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(40m, day.TollFee);
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateOverlappingTollFees_DayCapIs_60()
    {
        //Arrange
        var date = new DateTimeOffset(2025, 9, 25, 6, 0, 0, TimeSpan.Zero);
        var times = new[]
        {
            date,
            date.AddMinutes(30), 
            date.AddMinutes(70), 
            date.AddMinutes(90), 
            date.AddMinutes(140), 
            date.AddMinutes(160),
            date.AddMinutes(210), 
            date.AddMinutes(230)
        };

        var ordered = times
            .Select(dateTime => dateTime.UtcDateTime)
            .ToList();
        var repositoryResponse = new List<TollFeeResult>
        {
            new() { TollTime = times[0].UtcDateTime, TollFee = 20m },
            new() { TollTime = times[1].UtcDateTime, TollFee = 35m },
            new() { TollTime = times[2].UtcDateTime, TollFee = 30m },
            new() { TollTime = times[3].UtcDateTime, TollFee = 30m },
            new() { TollTime = times[4].UtcDateTime, TollFee = 30m },
            new() { TollTime = times[5].UtcDateTime, TollFee = 30m },
            new() { TollTime = times[6].UtcDateTime, TollFee = 30m },
            new() { TollTime = times[7].UtcDateTime, TollFee = 30m },
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
            .Returns(ApplicationResult.WithSuccess(repositoryResponse));

        //Act
        var result = _sut.CalculateOverlappingTollFees(ordered);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(60m, day.TollFee);
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateOverlappingTollFees_GroupsByDay_ReturnsSeparateEntries()
    {
        //Arrange
        var dayOne = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dayTwo = new DateTimeOffset(2025, 9, 26, 7, 0, 0, TimeSpan.Zero);
        var times = new[] { dayOne, dayOne.AddMinutes(30), dayTwo };
        var ordered = times
            .Select(dateTime => dateTime.UtcDateTime)
            .ToList();
        var repositoryResponse = new List<TollFeeResult>
        {
            new() { TollTime = times[0].UtcDateTime, TollFee = 20m },
            new() { TollTime = times[1].UtcDateTime, TollFee = 20m },
            new() { TollTime = times[2].UtcDateTime, TollFee = 35m },
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
            .Returns(ApplicationResult.WithSuccess(repositoryResponse));

        //Act
        var result = _sut.CalculateOverlappingTollFees(ordered);

        //Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Result.TollFees.Count);

        var dayOneResult = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 25));
        var dayTwoResult = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 26));

        Assert.Equal(20m, dayOneResult.TollFee);
        Assert.Equal(35m, dayTwoResult.TollFee);
        A.CallTo(() => _tollFeesRepository.GetTollFees(ordered))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateOverlappingTollFees_FetchFeesFailure_ReturnsUnSuccessful()
    {
        //Arrange
        var date = new DateTime(2025, 9, 25, 8, 0, 0);
        A.CallTo(() => _tollFeesRepository.GetTollFees(new[] { date }))
            .Returns(ApplicationResult.WithError<List<TollFeeResult>>("boom"));

        //Act
        var result = _sut.CalculateOverlappingTollFees(new[] { date });

        //Assert
        Assert.False(result.IsSuccessful);
        A.CallTo(() => _tollFeesRepository.GetTollFees(new[] { date }))
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
    public void GetTollFees_ShouldReturnTollFees_WhenRepositoryReturnsSuccess()
    {
        // Arrange
        var tollTime1 = DateTime.UtcNow;
        var tollTime2 = tollTime1.AddMinutes(30);
        var repositoryResponse = new ApplicationResult<List<TollFeeResult>>
        {
            Result = new List<TollFeeResult> { new() { TollFee = 10m }, new() { TollFee = 20m }, }
        };
        A.CallTo(() => _tollFeesRepository.GetTollFees(new[] { tollTime1, tollTime2 }))
            .Returns(repositoryResponse);


        var list = new List<DateTime> { tollTime1, tollTime2 };

        // Act
        var result = _sut.GetTollFees(list);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Result?.Count);
        Assert.Equal(10m, result.Result?[0].TollFee);
        Assert.Equal(20m, result.Result?[1].TollFee);
        A.CallTo(() => _tollFeesRepository.GetTollFees(new[] { tollTime1, tollTime2 }))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateDailyTollFees_SingleToll_ReturnsThatToll()
    {
        var tolls = new List<TollFeeResult>
        {
            new() { TollTime = new DateTime(2025, 9, 30), TollFee = 10 }
        };


        var result = _sut.CalculateDailyTollFees(tolls);

        Assert.Single(result);
        Assert.Equal(10, result[0]);
    }

    [Fact]
    public void CalculateDailyTollFees_MultipleTollsWithin60Minutes_ReturnsMaxTollOnly()
    {
        var tolls = new List<TollFeeResult>
        {
            new() { TollTime = new DateTime(2025, 9, 30), TollFee = 10 },
            new() { TollTime = new DateTime(2025, 9, 30), TollFee = 15 },
            new() { TollTime = new DateTime(2025, 9, 30), TollFee = 8 }
        };

        var result = _sut.CalculateDailyTollFees(tolls);

        Assert.Single(result);
        Assert.Equal(15, result[0]);
    }

    [Fact]
    public void CalculateDailyTollFees_MultipleTollsMoreThan60MinutesApart_ReturnsAllTolls()
    {
        var tolls = new List<TollFeeResult>
        {
            new() { TollTime = new DateTime(2025, 9, 30, 8, 15, 0), TollFee = 10 },
            new() { TollTime = new DateTime(2025, 9, 30, 12, 15, 0), TollFee = 15 }
        };

        var result = _sut.CalculateDailyTollFees(tolls);

        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0]);
        Assert.Equal(15, result[1]);
    }

    [Fact]
    public void CalculateDailyTollFees_MixedScenario_ReturnsCorrectTolls()
    {
        var tolls = new List<TollFeeResult>
        {
            new() { TollTime = new DateTime(2025, 9, 30, 8, 15, 0 ), TollFee = 10 },
            new() { TollTime = new DateTime(2025, 9, 30, 11, 15, 0), TollFee = 15 },
            new() { TollTime = new DateTime(2025, 9, 30, 13, 15, 0), TollFee = 8 },
            new() { TollTime = new DateTime(2025, 9, 30, 13, 15, 0), TollFee = 12 }
        };

        var sortedTollsPerDay = tolls
            .Select(t => new TollFeeResult { TollTime = t.TollTime, TollFee = t.TollFee })
            .OrderBy(t => t.TollTime)
            .ToList();

        var result = _sut.CalculateDailyTollFees(sortedTollsPerDay);

        Assert.Equal(3, result.Count);
        Assert.Equal(10, result[0]);
        Assert.Equal(15, result[1]);
        Assert.Equal(12, result[2]);
    }
}