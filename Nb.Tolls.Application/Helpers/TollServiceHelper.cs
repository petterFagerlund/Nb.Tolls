using Nb.Tolls.Domain.Enums;

namespace Nb.Tolls.Application.Helpers;

internal static class TollServiceHelper
{
    internal static bool IsTollFreeVehicle(Vehicle vehicle)
    {
        switch (vehicle)
        {
            case Vehicle.Car:
                return false;
            case Vehicle.Motorbike:
            case Vehicle.Tractor:
            case Vehicle.Emergency:
            case Vehicle.Diplomat:
            case Vehicle.Foreign:
            case Vehicle.Military:
                return true;
            default:
                return false;
        }
    }
}