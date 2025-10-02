using Nb.Tolls.Domain.Results;
using Nb.Tolls.WebApi.Host.Responses;

namespace Nb.Tolls.WebApi.Host.Mappers;

public static class TollFeesResponseMapper
{
    public static List<TollFeeResponse> Map(IEnumerable<TollFeeResult> tollFeeResults)
    {
        return tollFeeResults.Select(
                tollFee => new TollFeeResponse
                {
                    TollDate = DateOnly.FromDateTime(tollFee.TollFeeTime), TollFee = tollFee.TollFee
                })
            .ToList();
    }
}