namespace Nb.Tolls.WebApi.Host.Responses;
public record TollFeeResponse
{
    public required DateOnly TollDate { get; init; }
    public required int TollFee { get; init; }
}