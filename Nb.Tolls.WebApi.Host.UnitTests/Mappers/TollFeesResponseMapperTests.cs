using Nb.Tolls.Domain.Results;
using Nb.Tolls.WebApi.Host.Mappers;
using Xunit;

namespace Nb.Tolls.WebApi.Host.UnitTests.Mappers;

public class TollFeesResponseMapperTests
{
    [Fact]
    public void Map_GivenTollFeesResult_ReturnsMappedTollFeesResponse()
    {
        // Arrange
        var tollFeesResult = new DailyTollFeesResult
        {
            TollFees =
            [
                new DailyTollFeeResult { Date = new DateOnly(2025, 9, 30), TollFee = 15 },
                new DailyTollFeeResult { Date = new DateOnly(2025, 10, 1), TollFee = 8 }
            ]
        };

        // Act
        var response = TollFeesResponseMapper.Map(tollFeesResult);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.TollFees.Count);
        Assert.Equal(new DateOnly(2025, 9, 30), response.TollFees[0].TollDate);
        Assert.Equal(15, response.TollFees[0].TollFee);
        Assert.Equal(new DateOnly(2025, 10, 1), response.TollFees[1].TollDate);
        Assert.Equal(8, response.TollFees[1].TollFee);
    }

    [Fact]
    public void Map_GivenEmptyTollFeesResult_ReturnsEmptyTollFeesResponse()
    {
        // Arrange
        var tollFeesResult = new DailyTollFeesResult
        {
            TollFees = []
        };

        // Act
        var response = TollFeesResponseMapper.Map(tollFeesResult);

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.TollFees);
    }
}
