using Nb.Tolls.Domain.Enums;

namespace Nb.Tolls.WebApi.Models.Requests;

public class TollRequest
{
    public required Vehicle VehicleType { get; set; }
    public required DateTimeOffset[] TollTimes { get; set; }
}