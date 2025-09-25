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
    private readonly ITollFeeRepository _tollFeeRepository;
    private readonly ITollTimeService _tollTimeService;

    public TollFeesServiceTests()
    {
        _tollFeeRepository = A.Fake<ITollFeeRepository>();
        _tollTimeService = A.Fake<ITollTimeService>();
        var logger = A.Fake<ILogger<TollFeesService>>();
        _sut = new TollFeesService(_tollFeeRepository, _tollTimeService, logger);
    }

    [Fact]
    public void CalculateOverlappingTollFees_EmptyInput_ReturnsUnSuccessful()
    {
        //Arrange
        var input = Array.Empty<DateTimeOffset>();

        //Act
        var result = _sut.CalculateOverlappingTollFees(input);

        //Assert
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void CalculateOverlappingTollFees_SingleDay_ReturnsMaxFee()
    {
        //Arrange
        var baseDate = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var times = new[] { baseDate, baseDate.AddMinutes(30), baseDate.AddMinutes(45) };

        // return fees depending on minute
        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate.AddMinutes(30).UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(baseDate.AddMinutes(45).UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));

        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(20m, day.Fee);
    }

    [Fact]
    public void CalculateOverlappingTollFees_MultipleTimeWindowsSameDay_ReturnsCorrectFee()
    {
        //Arrange
        var date = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var times = new[] { date, date.AddMinutes(40), date.AddMinutes(70) };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(date.AddMinutes(40).UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 25m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(date.AddMinutes(70).UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));

        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(40m, day.Fee);
    }

    [Fact]
    public void CalculateOverlappingTollFees_DayCapIs_60()
    {
        //Arrange
        var date = new DateTimeOffset(2025, 9, 25, 6, 0, 0, TimeSpan.Zero);
        var times = new[]
        {
            date, date.AddMinutes(30), date.AddMinutes(70), date.AddMinutes(90), date.AddMinutes(140), date.AddMinutes(160),
            date.AddMinutes(210), date.AddMinutes(230)
        };

        foreach (var t in times)
        {
            var fee = t.Minute % 60 == 30 || t.Minute % 60 == 40 ? 20m : 10m;
            A.CallTo(() => _tollFeeRepository.GetTollFee(t.UtcDateTime))
                .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = fee }));
        }

        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(60m, day.Fee);
    }

    [Fact]
    public void CalculateOverlappingTollFees_GroupsByDay_ReturnsSeparateEntries()
    {
        //Arrange
        var dayOne = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dayTwo = new DateTimeOffset(2025, 9, 26, 7, 0, 0, TimeSpan.Zero);
        var times = new[] { dayOne, dayOne.AddMinutes(30), dayTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(dayOne.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dayOne.AddMinutes(30).UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dayTwo.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));

        //Act
        var result = _sut.CalculateOverlappingTollFees(times);

        //Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Result.TollFees.Count);

        var dayOneResult = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 25));
        var dayTwoResult = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 26));

        Assert.Equal(20m, dayOneResult.Fee);
        Assert.Equal(15m, dayTwoResult.Fee);
    }

    [Fact]
    public void CalculateOverlappingTollFees_FetchFeesFailure_ReturnsUnSuccessful()
    {
        //Arrange
        var date = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithError<TollFeeResult>("boom"));

        //Act
        var result = _sut.CalculateOverlappingTollFees(new[] { date });

        //Assert
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void FetchTollFees_EmptyInput_ReturnsUnSuccessful()
    {
        // Arrange
        var input = new List<DateTimeOffset>();

        // Act
        var result = _sut.FetchTollFees(input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
    }

    [Fact]
    public void FetchTollFees_AllOk_ReturnsSuccess_WithMappedEntries()
    {
        // Arrange
        var dateOne = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dateTwo = new DateTimeOffset(2025, 9, 25, 9, 15, 0, TimeSpan.Zero);
        var ordered = new List<DateTimeOffset> { dateOne, dateTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(dateOne.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));

        // Act
        var result = _sut.FetchTollFees(ordered);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Count);

        var tollTimeFeeResult = result.Result[0];
        var tollTimeFeeResultTwo = result.Result[1];

        Assert.Equal(dateOne, tollTimeFeeResult.TollTime);
        Assert.Equal(10m, tollTimeFeeResult.TollFee);

        Assert.Equal(dateTwo, tollTimeFeeResultTwo.TollTime);
        Assert.Equal(15m, tollTimeFeeResultTwo.TollFee);
    }

    [Fact]
    public void FetchTollFees_RepoFailureOnFirst_ReturnsUnSuccessful()
    {
        // Arrange
        var date = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dateTwo = new DateTimeOffset(2025, 9, 25, 8, 30, 0, TimeSpan.Zero);
        var ordered = new List<DateTimeOffset> { date, dateTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithError<TollFeeResult>("boom"));

        // Act
        var result = _sut.FetchTollFees(ordered);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);

        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo.UtcDateTime)).MustNotHaveHappened();
    }

    [Fact]
    public void FetchTollFees_RepoFailureOnSecond_ReturnsUnSuccessful()
    {
        // Arrange
        var date = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dateTwo = new DateTimeOffset(2025, 9, 25, 8, 30, 0, TimeSpan.Zero);
        var ordered = new List<DateTimeOffset> { date, dateTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo.UtcDateTime))
            .Returns(ApplicationResult.WithError<TollFeeResult>("fail-2"));

        // Act
        var result = _sut.FetchTollFees(ordered);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
    }

    [Fact]
    public void FetchTollFees_ZeroFee_IsIncludedInResults()
    {
        // Arrange
        var date = new DateTimeOffset(2025, 9, 25, 6, 0, 0, TimeSpan.Zero);
        var dateTwo = new DateTimeOffset(2025, 9, 25, 6, 30, 0, TimeSpan.Zero);
        var ordered = new List<DateTimeOffset> { date, dateTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 0m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));

        // Act
        var result = _sut.FetchTollFees(ordered);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Count);

        Assert.Equal(0m, result.Result[0].TollFee);
        Assert.Equal(15m, result.Result[1].TollFee);
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_EmptyInput_ReturnsUnSuccessful()
    {
        // Arrange
        var input = Array.Empty<DateTimeOffset>();

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
        var date = new DateTimeOffset(2025, 9, 25, 7, 0, 0, TimeSpan.Zero);
        var dateTwo = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var input = new[] { date, dateTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithError<TollFeeResult>("boom"));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo.UtcDateTime)).MustNotHaveHappened();
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_HappyPath_SumsPerDay_WithCap60()
    {
        // Arrange (same day)
        var date = new DateTimeOffset(2025, 9, 25, 6, 0, 0, TimeSpan.Zero);
        var input = new[] { date, date.AddMinutes(30), date.AddHours(2) };

        A.CallTo(() => _tollFeeRepository.GetTollFee(input[0].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[1].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 25m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[2].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 30m }));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(new DateOnly(2025, 9, 25), day.Date);
        Assert.Equal(60m, day.Fee); // capped
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_MultipleDays_ReturnsSeparateEntries()
    {
        var date = new DateTimeOffset(2025, 9, 25, 7, 0, 0, TimeSpan.Zero);
        var dateWithDifferentTime = new DateTimeOffset(2025, 9, 25, 8, 0, 0, TimeSpan.Zero);
        var dateThree = new DateTimeOffset(2025, 9, 26, 9, 0, 0, TimeSpan.Zero);

        var input = new[] { date, dateWithDifferentTime, dateThree };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateWithDifferentTime.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateThree.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 40m }));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Result.TollFees.Count);

        var day1 = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 25));
        var day2 = result.Result.TollFees.First(x => x.Date == new DateOnly(2025, 9, 26));

        Assert.Equal(25m, day1.Fee);
        Assert.Equal(40m, day2.Fee);
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_AllZeroFees_ReturnsNotFound()
    {
        var date = new DateTimeOffset(2025, 9, 25, 6, 0, 0, TimeSpan.Zero);
        var dateTwo = new DateTimeOffset(2025, 9, 25, 7, 0, 0, TimeSpan.Zero);
        var input = new[] { date, dateTwo };

        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 0m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 0m }));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
    }

    [Fact]
    public void CalculateNonOverlappingTollFees_PartialZeroFees_SumsNonZeroAndCaps()
    {
        var date = new DateTimeOffset(2025, 9, 25, 6, 0, 0, TimeSpan.Zero);
        var input = new[] { date, date.AddMinutes(15), date.AddHours(1) };

        A.CallTo(() => _tollFeeRepository.GetTollFee(input[0].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 0m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[1].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 25m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[2].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 45m }));

        // Act
        var result = _sut.CalculateNonOverlappingTollFees(input);

        // Assert
        Assert.True(result.IsSuccessful);
        var day = Assert.Single(result.Result.TollFees);
        Assert.Equal(60m, day.Fee);
    }

    [Fact]
    public async Task GetTollFees_TollFreeVehicle_ReturnsEmptySuccess()
    {
        // Arrange
        const Vehicle vehicle = Vehicle.Motorbike;
        var input = Array.Empty<DateTimeOffset>();

        // Act
        var result =await _sut.GetTollFees(vehicle, input);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result.TollFees);
        A.CallTo(() => _tollTimeService.ExtractEligibleTollFeeTimes(A<List<DateTimeOffset>>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetTollFees_NonOverlappingCalculationFails_ReturnsError()
    {
        // Arrange
        const Vehicle vehicle = Vehicle.Car;
        var t0 = new DateTimeOffset(2025, 9, 25, 08, 00, 00, TimeSpan.Zero);

        // pipeline
        var input = new[] { t0 };
        var eligible = new List<DateTimeOffset> { t0 };
        var nonOverlapping = new List<DateTimeOffset> { t0 };

        A.CallTo(() => _tollTimeService.ExtractEligibleTollFeeTimes(A<List<DateTimeOffset>>._))
            .Returns(eligible);
        A.CallTo(() => _tollTimeService.ExtractNonOverlappingTollTimes(eligible))
            .Returns(nonOverlapping);
        A.CallTo(() => _tollTimeService.ExtractOverlappingTollTimes(eligible))
            .Returns(new List<DateTimeOffset>());

        A.CallTo(() => _tollFeeRepository.GetTollFee(t0.UtcDateTime))
            .Returns(ApplicationResult.WithError<TollFeeResult>("repo-fail"));

        // Act
        var result = await _sut.GetTollFees(vehicle, input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
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
        var eligible = new List<DateTimeOffset> { date, dateTwo };

        A.CallTo(() => _tollTimeService.ExtractEligibleTollFeeTimes(A<List<DateTimeOffset>>._))
            .Returns(eligible);
        A.CallTo(() => _tollTimeService.ExtractNonOverlappingTollTimes(eligible))
            .Returns(new List<DateTimeOffset> { dateTwo });
        A.CallTo(() => _tollTimeService.ExtractOverlappingTollTimes(eligible))
            .Returns(new List<DateTimeOffset> { date });

        A.CallTo(() => _tollFeeRepository.GetTollFee(dateTwo.UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));

        A.CallTo(() => _tollFeeRepository.GetTollFee(date.UtcDateTime))
            .Returns(ApplicationResult.WithError<TollFeeResult>("repo-fail-overlap"));

        // Act
        var result = await _sut.GetTollFees(vehicle, input);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task GetTollFees_HappyPath_MergesResultsAndReturnsSuccess()
    {
        // Arrange
        const Vehicle vehicle = Vehicle.Car;
        var date = new DateTimeOffset(2025, 9, 25, 07, 00, 00, TimeSpan.Zero);

        var input = new[]
        {
            date,
            date.AddMinutes(30),
            date.AddHours(2),
        };
        var eligible = input.ToList();

        A.CallTo(() => _tollTimeService.ExtractEligibleTollFeeTimes(A<List<DateTimeOffset>>._))
            .Returns(eligible);

        A.CallTo(() => _tollTimeService.ExtractNonOverlappingTollTimes(eligible))
            .Returns(new List<DateTimeOffset> { input[2] });

        A.CallTo(() => _tollTimeService.ExtractOverlappingTollTimes(eligible))
            .Returns(new List<DateTimeOffset> { input[0], input[1] });

        A.CallTo(() => _tollFeeRepository.GetTollFee(input[2].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 15m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[0].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 10m }));
        A.CallTo(() => _tollFeeRepository.GetTollFee(input[1].UtcDateTime))
            .Returns(ApplicationResult.WithSuccess(new TollFeeResult { TollFee = 20m }));

        // Act
        var result = await _sut.GetTollFees(vehicle, input);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Result);

        Assert.Equal(2, result.Result.TollFees.Count);

        var total = result.Result.TollFees.Sum(x => x.Fee);
        Assert.Equal(35m, total);
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