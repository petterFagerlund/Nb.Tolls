namespace Nb.Tolls.WebApi.Models.Responses;

public class TollFeesResponse
{
    public required List<TollFeeResponse> TollFees { get; init; } = [];
}