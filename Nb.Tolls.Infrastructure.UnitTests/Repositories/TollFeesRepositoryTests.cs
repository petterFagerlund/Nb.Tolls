using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Infrastructure.Configuration;
using Nb.Tolls.Infrastructure.Models;
using Nb.Tolls.Infrastructure.Repositories.Implementations;
using Xunit;

namespace Nb.Tolls.Infrastructure.UnitTests.Repositories;

public class TollFeesRepositoryTests
{
    private readonly TollFeesRepository _sut;

    public TollFeesRepositoryTests()
    {
        var logger = A.Fake<ILogger<TollFeesRepository>>();

        var fakeTollFees = new List<TollFeesModel>
        {
            new() { StartMin = 360, EndMin = 390, AmountSek = 8, Start = "06:00", End = "06:30" },
            new() { StartMin = 420, EndMin = 480, AmountSek = 18, Start = "07:00", End = "08:00" },
            new() { StartMin = 900, EndMin = 930, AmountSek = 13, Start = "15:00", End = "15:30" }
        };

        var configurationLoader = A.Fake<ITollFeesConfigurationLoader>();
        A.CallTo(() => configurationLoader.LoadFromDataFolder()).Returns(fakeTollFees);

        var configuration = A.Fake<IConfiguration>();
        A.CallTo(() => configuration["TollSettings:TimeZone"]).Returns("Europe/Stockholm");

        _sut = new TollFeesRepository(configurationLoader, configuration, logger);
    }

    [Theory]
    [InlineData(6, 15, 8)]
    [InlineData(7, 30, 18)]
    [InlineData(15, 15, 13)]
    public void GetTollFee_ReturnsCorrectFee_ForGivenStockholmTime(int hour, int minute, decimal expectedFee)
    {
        // Arrange
        var stockholmTime = new DateTime(2025, 9, 27, hour, minute, 0);
        var stockholmTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        var utcTime = TimeZoneInfo.ConvertTimeToUtc(stockholmTime, stockholmTimeZone);

        // Act
        var result = _sut.GetTollFees(new []{utcTime});

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(expectedFee, result.Result.First().TollFee);
    }

    [Fact]
    public void GetTollFee_ReturnsNotFound_WhenNoMatchingRule()
    {
        // Arrange
        var stockholmTime = new DateTime(2025, 9, 27, 9, 0, 0);
        var stockholmTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        var utcTime = TimeZoneInfo.ConvertTimeToUtc(stockholmTime, stockholmTimeZone);

        // Act
        var result = _sut.GetTollFees(new []{utcTime});

        // Assert
        Assert.Equal(ApplicationResultStatus.NotFound, result.ApplicationResultStatus);
    }
}
