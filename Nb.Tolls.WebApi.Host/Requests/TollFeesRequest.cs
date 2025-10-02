using Nb.Tolls.Domain.Enums;

namespace Nb.Tolls.WebApi.Host.Requests;

public record TollFeesRequest
{
    public required Vehicle VehicleType { get; init; }
    public required DateTimeOffset[] TollTimes { get; init; }
}