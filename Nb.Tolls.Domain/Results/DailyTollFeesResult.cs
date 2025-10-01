namespace Nb.Tolls.Domain.Results;

public class DailyTollFeesResult
{
    public required List<TollFeeResult>? TollFees { get; init; } = new();
}