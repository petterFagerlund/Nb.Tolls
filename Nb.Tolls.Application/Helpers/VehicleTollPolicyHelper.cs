using Nb.Tolls.Domain.Enums;

namespace Nb.Tolls.Application.Helpers;
internal static class VehicleTollPolicyHelper
{
    private static readonly HashSet<Vehicle> TollFreeVehicles =
    [
        Vehicle.Motorbike,
        Vehicle.Tractor,
        Vehicle.Emergency,
        Vehicle.Diplomat,
        Vehicle.Foreign,
        Vehicle.Military
    ];
    
    internal static bool IsTollFreeVehicle(Vehicle vehicle) =>
        TollFreeVehicles.Contains(vehicle);
}