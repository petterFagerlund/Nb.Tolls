namespace Nb.Tolls.WebApi.Models.Responses;

public class DailyTollFeeResponse
{
    public required DateOnly Date { get; init; }
    public required decimal Fee { get; set; }
}