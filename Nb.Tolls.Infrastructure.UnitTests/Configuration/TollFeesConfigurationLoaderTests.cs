using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Nb.Tolls.Infrastructure.Configuration;
using Xunit;

namespace Nb.Tolls.Infrastructure.UnitTests.Configuration;

public class TollFeesConfigurationLoaderTests
{
    private readonly TollFeesConfigurationLoader _sut;

    public TollFeesConfigurationLoaderTests()
    {
        var configuration = A.Fake<IConfiguration>();
        _sut = new TollFeesConfigurationLoader(configuration);
    }

    [Fact]
    public void LoadRules_ValidJson_ReturnsOrderedTollFees()
    {
        // Arrange
        var json = @"{
        ""timezone"": ""Europe/Stockholm"",
        ""semantics"": ""start-inclusive, end-exclusive; midnight span split into two rules"",
        ""TollFees"": [
            { ""startMin"": 60, ""endMin"": 120, ""amountSek"": 10, ""start"": ""01:00"", ""end"": ""02:00"" },
            { ""startMin"": 0, ""endMin"": 60, ""amountSek"": 5, ""start"": ""00:00"", ""end"": ""01:00"" }
        ]
    }";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        // Act
        var result = _sut.LoadRules(tempFile);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].StartMin);
        Assert.Equal(60, result[1].StartMin);

        File.Delete(tempFile);
    }

    [Fact]
    public void LoadRules_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "not a valid json";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, invalidJson);

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => _sut.LoadRules(tempFile));

        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFromDataFolder_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "TollFeesData.json");

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => _sut.LoadFromDataFolder());
        Assert.Contains("Toll fees configuration file not found", ex.Message);
    }

    [Fact]
    public void LoadRules_EmptyTollFees_ReturnsEmptyList()
    {
        // Arrange
        var json = @"{
        ""timezone"": ""Europe/Stockholm"",
        ""semantics"": ""start-inclusive, end-exclusive; midnight span split into two rules"",
        ""TollFees"": []
    }";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        // Act
        var result = _sut.LoadRules(tempFile);

        // Assert
        Assert.Empty(result);

        // Cleanup
        File.Delete(tempFile);
    }
}