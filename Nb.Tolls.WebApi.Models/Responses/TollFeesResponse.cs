namespace Nb.Tolls.WebApi.Models.Responses;

public class TollFeesResponse
{
    public required List<DailyTollFeeResponse> TollFees { get; init; } = [];
}