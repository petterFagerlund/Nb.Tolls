namespace Nb.Tolls.Domain.Results;

public class DailyTollFeeResult
{
    public required DateOnly Date { get; init; }
    public required decimal TollFee { get; set; }
}