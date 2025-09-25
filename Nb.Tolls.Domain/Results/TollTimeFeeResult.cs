namespace Nb.Tolls.Domain.Results;

public class TollTimeFeeResult
{
    public DateTimeOffset TollTime { get; init; }
    public decimal TollFee { get; init; }
}