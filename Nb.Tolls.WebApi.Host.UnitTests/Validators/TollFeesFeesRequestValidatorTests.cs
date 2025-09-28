using Nb.Tolls.WebApi.Host.Validators.Implementation;
using Xunit;

namespace Nb.Tolls.WebApi.Host.UnitTests.Validators;

public class TollRequestValidatorTests
{
    private readonly TollFeesFeesRequestValidator _sut;

    public TollRequestValidatorTests()
    {
        _sut = new TollFeesFeesRequestValidator();
    }

    [Fact]
    public void ValidateTollTimes_WithEmptyArray_ReturnsModelError()
    {
        // Arrange
        var tollTimes = Array.Empty<DateTimeOffset>();

        // Act
        var result = _sut.ValidateTollTimes(tollTimes);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.ContainsKey(nameof(tollTimes)));
        var errors = result[nameof(tollTimes)]?.Errors;
        Assert.Single(errors!);
        Assert.Equal("At least one toll time must be provided.", errors![0].ErrorMessage);
    }

    [Fact]
    public void ValidateTollTimes_WithFutureDate_AddsModelError()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddHours(1);
        var tollTimes = new[] { futureTime };

        // Act
        var result = _sut.ValidateTollTimes(tollTimes);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(nameof(tollTimes), result.Keys);
        Assert.Contains(result[nameof(tollTimes)]?.Errors!, e => e.ErrorMessage.Contains("must not be in the future"));
    }

    [Fact]
    public void ValidateTollTimes_WithMaxOrMinDate_AddsModelError()
    {
        // Arrange
        var tollTimes = new[] { DateTimeOffset.MaxValue, DateTimeOffset.MinValue };

        // Act
        var result = _sut.ValidateTollTimes(tollTimes);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(nameof(tollTimes), result.Keys);
        Assert.Equal(3, result[nameof(tollTimes)]!.Errors.Count);
    }

    [Fact]
    public void ValidateTollTimes_WithValidDates_ReturnsValidModelState()
    {
        // Arrange
        var tollTimes = new[] { DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddMinutes(-5) };

        // Act
        var result = _sut.ValidateTollTimes(tollTimes);

        // Assert
        Assert.True(result.IsValid);
    }
}