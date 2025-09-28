namespace Nb.Tolls.Domain.Results;

public class DailyTollFeesResult
{
    public required List<DailyTollFeeResult> TollFees { get; init; }
}