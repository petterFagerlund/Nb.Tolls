namespace Nb.Tolls.WebApi.Models.Responses;

public class DailyTollFeeResponse
{
    public required DateOnly TollDate { get; init; }
    public required decimal TollFee { get; init; }
}