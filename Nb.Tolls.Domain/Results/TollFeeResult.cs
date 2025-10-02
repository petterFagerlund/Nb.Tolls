namespace Nb.Tolls.Domain.Results;

public record TollFeeResult
{
    public DateTime TollFeeTime { get; init; }
    public int TollFee { get; init; }
}