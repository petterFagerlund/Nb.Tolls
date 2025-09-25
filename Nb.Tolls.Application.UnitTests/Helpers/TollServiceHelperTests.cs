using Nb.Tolls.Application.Helpers;
using Nb.Tolls.Domain.Enums;
using Xunit;

namespace Nb.Tolls.Application.UnitTests.Helpers;

public class TollServiceHelperTests
{
    [Theory]
    [InlineData(Vehicle.Car, false)]
    [InlineData(Vehicle.Motorbike, true)]
    [InlineData(Vehicle.Tractor, true)]
    [InlineData(Vehicle.Emergency, true)]
    [InlineData(Vehicle.Diplomat, true)]
    [InlineData(Vehicle.Foreign, true)]
    [InlineData(Vehicle.Military, true)]
    public void IsTollFreeVehicle_ReturnsExpectedResult(Vehicle vehicle, bool expected)
    {
        // Act
        var result = TollServiceHelper.IsTollFreeVehicle(vehicle);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsTollFreeVehicle_UnknownVehicle_ReturnsFalse()
    {
        // Arrange
        const Vehicle unknownVehicle = (Vehicle)999;

        // Act
        var result = TollServiceHelper.IsTollFreeVehicle(unknownVehicle);

        // Assert
        Assert.False(result);
    }
}