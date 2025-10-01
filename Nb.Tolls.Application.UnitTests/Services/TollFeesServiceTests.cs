using FakeItEasy;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Application.Services;
using Nb.Tolls.Application.Services.Implementations;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;
using Xunit;

namespace Nb.Tolls.Application.UnitTests.Services;

public class TollFeesServiceTests
{
    private readonly TollFeesService _sut;
    private readonly ITollTimeService _tollTimeService;
    private readonly ITollFeesRepository _tollFeeRepository;

    public TollFeesServiceTests()
    {
        _tollTimeService = A.Fake<ITollTimeService>();
        var logger = A.Fake<ILogger<TollFeesService>>();
        _tollFeeRepository = A.Fake<ITollFeesRepository>();
        _sut = new TollFeesService(_tollFeeRepository, _tollTimeService, logger);
    }

    [Fact]
    public async Task GetTollFees_EmptyInput_ReturnsUnSuccessful()
    {
        // Arrange
        var input = Array.Empty<DateTimeOffset>();

        // Act
        var result = await _sut.GetTollFees(Vehicle.Car, input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task GetTollFees_AllOk_ReturnsSuccess_WithMappedEntries()
    {
        // Arrange
        var dateOne = new DateTime(2025, 9, 25, 8, 0, 0);
        var dateTwo = new DateTime(2025, 9, 25, 9, 15, 0);
        var ordered = new[] { dateOne, dateTwo };

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .Returns(ordered.ToList());
        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(A<List<DateTime>>._))
            .Returns(ordered.ToList());
        A.CallTo(() => _tollFeeRepository.GetTollFees(ordered))
            .Returns(
                new ApplicationResult<List<TollFeeResult>>
                {
                    Result = new List<TollFeeResult>
                    {
                        new() { TollFeeTime = dateOne, TollFee = 10m }, new() { TollFeeTime = dateTwo, TollFee = 15m }
                    }
                });
        var orderedInput = ordered
            .Select(d => new DateTimeOffset(d, TimeSpan.Zero))
            .ToArray();
        // Act
        var result = await _sut.GetTollFees(Vehicle.Car, orderedInput);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        Assert.Single(result.Result.TollFees!);

        var tollTimeFeeResult = result.Result.TollFees![0];

        Assert.Equal(DateOnly.FromDateTime(dateOne), DateOnly.FromDateTime(tollTimeFeeResult.TollFeeTime));
        Assert.Equal(25m, tollTimeFeeResult.TollFee);

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTollFees_ZeroFee_IsIncludedInResults()
    {
        // Arrange
        var date = new DateTime(2025, 9, 25, 6, 0, 0);
        var dateTwo = new DateTime(2025, 9, 26, 6, 30, 0);
        var ordered = new[] { date, dateTwo };

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .Returns(ordered.ToList());
        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(A<List<DateTime>>._))
            .Returns(ordered.ToList());
        
        A.CallTo(() => _tollFeeRepository.GetTollFees(ordered))
            .Returns(
                new ApplicationResult<List<TollFeeResult>>
                {
                    Result = new List<TollFeeResult>
                    {
                        new() { TollFeeTime = ordered[0], TollFee = 0m },
                        new() { TollFeeTime = ordered[1], TollFee = 0m },

                    }
                });

        var orderedInput = ordered
            .Select(d => new DateTimeOffset(d, TimeSpan.Zero))
            .ToArray();

        // Act
        var result = await _sut.GetTollFees(Vehicle.Car, orderedInput);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        Assert.True(result.Result.TollFees!.Count == 2);

        Assert.Equal(0m, result.Result.TollFees![1].TollFee);
        Assert.Equal(0m, result.Result.TollFees![0].TollFee);

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _tollFeeRepository.GetTollFees(ordered))
            .MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task GetTollFees_TollFreeVehicle_ReturnsEmptySuccess()
    {
        // Arrange
        const Vehicle vehicle = Vehicle.Motorbike;
        var input = Array.Empty<DateTimeOffset>();

        // Act
        var result = await _sut.GetTollFees(vehicle, input);

        // Assert
        Assert.False(result.IsSuccessful);
        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetTollFees_NonOverlappingCalculationFails_ReturnsError()
    {
        // Arrange
        const Vehicle vehicle = Vehicle.Car;
        var dateTime = new DateTime(2025, 9, 25, 08, 00, 00);

        // pipeline
        var input = new[] { dateTime };
        var eligible = new List<DateTime> { dateTime };
        var nonOverlapping = new List<DateTime> { dateTime };

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .Returns(eligible);
        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(eligible))
            .Returns(nonOverlapping);

        var inputAsDateTimeOffsets = input
            .Select(d => new DateTimeOffset(d, TimeSpan.Zero))
            .ToArray();

        // Act
        var result = await _sut.GetTollFees(vehicle, inputAsDateTimeOffsets);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(eligible))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTollFees_OverlappingCalculationFails_ReturnsError()
    {
        // Arrange
        const Vehicle vehicle = Vehicle.Car;
        var date = new DateTimeOffset(2025, 9, 25, 08, 00, 00, TimeSpan.Zero);
        var dateTwo = new DateTimeOffset(
            2025,
            9,
            25,
            09,
            30,
            00,
            TimeSpan.Zero);

        var input = new[] { date, dateTwo };
        var eligible = new List<DateTime> { date.UtcDateTime, dateTwo.UtcDateTime };

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .Returns(eligible);
        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(eligible))
            .Returns([]);
        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(eligible))
            .Returns([date.UtcDateTime, dateTwo.UtcDateTime]);

        // Act
        var result = await _sut.GetTollFees(vehicle, input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(eligible))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(eligible))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTollFees_HappyPath_MergesResultsAndReturnsSuccess()
    {
        // Arrange
        const Vehicle vehicle = Vehicle.Car;
        var date = new DateTime(2025, 9, 25, 07, 00, 00);

        var input = new[] { date, date.AddMinutes(30), date.AddHours(2), };
        var eligible = input.ToList();

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .Returns(eligible);

        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(eligible))
            .Returns([input[2]]);

        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(eligible))
            .Returns([input[0], input[1]]);

        A.CallTo(() => _tollFeeRepository.GetTollFees(A<List<DateTime>>.That.Contains(input[2])))
            .Returns(
                new ApplicationResult<List<TollFeeResult>>
                {
                    Result = new List<TollFeeResult> { new() { TollFeeTime = input[2], TollFee = 15m }, }
                });

        A.CallTo(
                () => _tollFeeRepository.GetTollFees(
                    A<List<DateTime>>.That.Matches(
                        list =>
                            list.Contains(input[0]) && list.Contains(input[1])
                    )
                ))
            .Returns(
                new ApplicationResult<List<TollFeeResult>>
                {
                    Result = new List<TollFeeResult>
                    {
                        new() { TollFeeTime = input[0], TollFee = 10m }, new() { TollFeeTime = input[1], TollFee = 10m },
                    }
                });

        var inputAsDateTimeOffsets = input
            .Select(d => new DateTimeOffset(d, TimeSpan.Zero))
            .ToArray();

        // Act
        var result = await _sut.GetTollFees(vehicle, inputAsDateTimeOffsets);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);

        Assert.Equal(2, result.Result.TollFees!.Count);

        var total = result.Result.TollFees.Sum(x => x.TollFee);
        Assert.Equal(25m, total);

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(eligible))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(eligible))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTollFees_NoFeesFound_ReturnsNotFound()
    {
        const Vehicle vehicle = Vehicle.Car;
        var input = Array.Empty<DateTimeOffset>();
        var result = await _sut.GetTollFees(vehicle, input);

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
    }
    
     [Fact]
    public void CalculateDailyTollFees_ShouldReturnSingleToll_WhenOnlyOneToll()
    {
        // Arrange
        var tolls = new List<TollFeeResult>
        {
            new() { TollFeeTime = DateTime.UtcNow, TollFee = 10 }
        };

        // Act
        var result = _sut.CalculateDailyTollFees(tolls);

        // Assert
        Assert.Single(result);
        Assert.Equal(10, result[0]);
    }

    [Fact]
    public void CalculateDailyTollFees_ShouldTakeMaxTollWithin60MinutesWindow()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var tolls = new List<TollFeeResult>
        {
            new() { TollFeeTime = baseTime, TollFee = 10 },
            new() { TollFeeTime = baseTime.AddMinutes(30), TollFee = 20 },
            new() { TollFeeTime = baseTime.AddMinutes(90), TollFee = 15 }
        };

        // Act
        var result = _sut.CalculateDailyTollFees(tolls);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(20, result[0]);
        Assert.Equal(15, result[1]);
    }

    [Fact]
    public void CalculateDailyTollFees_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        // Arrange
        var tolls = new List<TollFeeResult>();

        // Act
        var result = _sut.CalculateDailyTollFees(tolls);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateTollFees_ShouldReturnError_WhenRepositoryFails()
    {
        // Arrange
        var inputTimes = new List<DateTime> { DateTime.UtcNow };
        var repoResult = ApplicationResult.WithError<List<TollFeeResult>>("fail");
        A.CallTo(() => _tollFeeRepository.GetTollFees(inputTimes)).Returns(repoResult);

        // Act
        var result = _sut.CalculateTollFees(inputTimes);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("Failed to fetch toll fees for overlapping times.", result.Messages.First());
    }

    [Fact]
    public void CalculateTollFees_ShouldReturnNotFound_WhenNoTolls()
    {
        // Arrange
        var inputTimes = new List<DateTime> { DateTime.UtcNow };
        var repoResult = ApplicationResult.WithSuccess(new List<TollFeeResult>());
        A.CallTo(() => _tollFeeRepository.GetTollFees(inputTimes)).Returns(repoResult);

        // Act
        var result = _sut.CalculateTollFees(inputTimes);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(ApplicationResultStatus.NotFound, result.ApplicationResultStatus);
    }

    [Fact]
    public void CalculateTollFees_ShouldReturnAggregatedDailyTolls()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var inputTimes = new List<DateTime> { today, today.AddHours(2), today.AddDays(1) };
        var tollsFromRepo = new List<TollFeeResult>
        {
            new() { TollFeeTime = today.AddHours(1), TollFee = 10 },
            new() { TollFeeTime = today.AddHours(2), TollFee = 20 },
            new() { TollFeeTime = today.AddDays(1).AddHours(1), TollFee = 15 }
        };
        var repoResult = ApplicationResult.WithSuccess(tollsFromRepo);
        A.CallTo(() => _tollFeeRepository.GetTollFees(inputTimes)).Returns(repoResult);

        // Act
        var result = _sut.CalculateTollFees(inputTimes);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Result.Count);
        Assert.Equal(today, result.Result[0].TollFeeTime.Date);
        Assert.Equal(30, result.Result[0].TollFee);
        Assert.Equal(today.AddDays(1), result.Result[1].TollFeeTime.Date);
        Assert.Equal(15, result.Result[1].TollFee);
    }
}