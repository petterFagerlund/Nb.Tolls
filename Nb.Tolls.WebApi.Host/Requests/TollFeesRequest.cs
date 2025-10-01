using Nb.Tolls.Domain.Enums;

namespace Nb.Tolls.WebApi.Host.Requests;

public record TollFeesRequest
{
    public required Vehicle VehicleType { get; set; }
    public required DateTimeOffset[] TollTimes { get; set; }
}