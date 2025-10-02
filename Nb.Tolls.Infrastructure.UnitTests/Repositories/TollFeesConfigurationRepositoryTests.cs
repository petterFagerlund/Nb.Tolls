using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Nb.Tolls.Infrastructure.Repositories.Implementations;
using Xunit;

namespace Nb.Tolls.Infrastructure.UnitTests.Repositories;

public class TollFeesConfigurationRepositoryTests
{
    private readonly TollFeesConfigurationRepository _sut;

    public TollFeesConfigurationRepositoryTests()
    {
        var configuration = A.Fake<IConfiguration>();
        _sut = new TollFeesConfigurationRepository(configuration);
    }

    [Fact]
    public void GetTollFeeModels_ValidJson_ReturnsOrderedTollFees()
    {
        // Arrange
        var json = @"[
        {
            ""Start"": ""06:00"",
            ""End"": ""06:29"",
            ""AmountSek"": 8,
            ""StartMin"": 360,
            ""EndMin"": 389
        },
        {
            ""Start"": ""06:30"",
            ""End"": ""06:59"",
            ""AmountSek"": 13,
            ""StartMin"": 390,
            ""EndMin"": 419
        }
    ]";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        // Act
        var result = _sut.GetTollFeeModels(tempFile);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(360, result[0].StartMin);
        Assert.Equal(390, result[1].StartMin);

        File.Delete(tempFile);
    }

    [Fact]
    public void GetTollFeeModels_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "not a valid json";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, invalidJson);

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => _sut.GetTollFeeModels(tempFile));

        File.Delete(tempFile);
    }

    [Fact]
    public void GetTollFees_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "TollFeesData.json");

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => _sut.GetTollFees());
        Assert.Contains("Toll fees configuration file not found", ex.Message);
    }

    [Fact]
    public void GetTollFeeModels_EmptyTollFees_ReturnsEmptyList()
    {
        // Arrange
        const string json = "[]";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        // Act
        var result = _sut.GetTollFeeModels(tempFile);

        // Assert
        Assert.Empty(result);

        // Cleanup
        File.Delete(tempFile);
    }
}