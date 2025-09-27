using FakeItEasy;
using Microsoft.Extensions.Logging;
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
    private readonly ITollFeesCalculationService _tollFeesCalculationService;

    public TollFeesServiceTests()
    {
        _tollFeesCalculationService = A.Fake<ITollFeesCalculationService>();
        _tollTimeService = A.Fake<ITollTimeService>();
        var logger = A.Fake<ILogger<TollFeesService>>();
        _sut = new TollFeesService(_tollFeesCalculationService, _tollTimeService, logger);
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
        A.CallTo(() => _tollFeesCalculationService.CalculateNonOverlappingTollFees(ordered))
            .Returns(
                new ApplicationResult<TollFeesResult>
                {
                    Result = new TollFeesResult
                    {
                        TollFees =
                        [
                            new DailyTollFeeResult { Date = DateOnly.FromDateTime(dateTwo), TollFee = 10m },
                            new DailyTollFeeResult { Date = DateOnly.FromDateTime(dateOne), TollFee = 15m }
                        ]
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
        Assert.Equal(2, result.Result.TollFees.Count);

        var tollTimeFeeResult = result.Result.TollFees[0];
        var tollTimeFeeResultTwo = result.Result.TollFees[1];

        Assert.Equal(DateOnly.FromDateTime(dateOne), tollTimeFeeResult.Date);
        Assert.Equal(10m, tollTimeFeeResult.TollFee);

        Assert.Equal(DateOnly.FromDateTime(dateTwo), tollTimeFeeResultTwo.Date);
        Assert.Equal(15m, tollTimeFeeResultTwo.TollFee);

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeesCalculationService.CalculateNonOverlappingTollFees(ordered))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTollFees_ZeroFee_IsIncludedInResults()
    {
        // Arrange
        var date = new DateTime(2025, 9, 25, 6, 0, 0);
        var dateTwo = new DateTime(2025, 9, 25, 6, 30, 0);
        var ordered = new[] { date, dateTwo };

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .Returns(ordered.ToList());
        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(A<List<DateTime>>._))
            .Returns(ordered.ToList());
        A.CallTo(() => _tollFeesCalculationService.CalculateOverlappingTollFees(ordered))
            .Returns(
                new ApplicationResult<TollFeesResult>
                {
                    Result = new TollFeesResult
                    {
                        TollFees =
                        [
                            new DailyTollFeeResult { Date = DateOnly.FromDateTime(dateTwo), TollFee = 0 },
                            new DailyTollFeeResult { Date = DateOnly.FromDateTime(date), TollFee = 15m }
                        ]
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
        Assert.Equal(2, result.Result.TollFees.Count);

        Assert.Equal(0m, result.Result.TollFees[0].TollFee);
        Assert.Equal(15m, result.Result.TollFees[1].TollFee);

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _tollFeesCalculationService.CalculateOverlappingTollFees(ordered))
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
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result.TollFees);
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
            00, TimeSpan.Zero);

        var input = new[] { date, dateTwo };
        var eligible = new List<DateTime> { date.UtcDateTime, dateTwo.UtcDateTime };

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .Returns(eligible);
        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(eligible))
            .Returns(new List<DateTime>());
        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(eligible))
            .Returns(new List<DateTime> { date.UtcDateTime, dateTwo.UtcDateTime });

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
            .Returns(new List<DateTime> { input[2] });

        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(eligible))
            .Returns(new List<DateTime> { input[0], input[1] });

        A.CallTo(() => _tollFeesCalculationService.CalculateOverlappingTollFees(A<List<DateTime>>._))
            .Returns(
                new ApplicationResult<TollFeesResult>
                {
                    Result = new TollFeesResult
                    {
                        TollFees =
                        [
                            new DailyTollFeeResult { Date = DateOnly.FromDateTime(input[1]), TollFee = 15m },
                        ]
                    }
                });

        A.CallTo(() => _tollFeesCalculationService.CalculateNonOverlappingTollFees(A<List<DateTime>>._))
            .Returns(
                new ApplicationResult<TollFeesResult>
                {
                    Result = new TollFeesResult
                    {
                        TollFees =
                        [
                            new DailyTollFeeResult { Date = DateOnly.FromDateTime(input[2]), TollFee = 10m },
                        ]
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

        Assert.Equal(2, result.Result.TollFees.Count);

        var total = result.Result.TollFees.Sum(x => x.TollFee);
        Assert.Equal(25m, total);

        A.CallTo(() => _tollTimeService.GetEligibleTollFeeTimes(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _tollTimeService.GetNonOverlappingTollTimes(eligible))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _tollTimeService.GetOverlappingTollTimes(eligible))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _tollFeesCalculationService.CalculateOverlappingTollFees(A<List<DateTime>>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _tollFeesCalculationService.CalculateNonOverlappingTollFees(A<List<DateTime>>._))
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
}