using Nb.Tolls.Domain.Results;
using Nb.Tolls.WebApi.Host.Responses;

namespace Nb.Tolls.WebApi.Host.Mappers;

public static class TollFeesResponseMapper
{
    public static List<TollFeeResponse> Map(DailyTollFeesResult dailyTollFeesResult)
    {
        return dailyTollFeesResult.TollFees!.Select(
                tollFee => new TollFeeResponse
                {
                    TollDate = DateOnly.FromDateTime(tollFee.TollFeeTime), TollFee = tollFee.TollFee
                })
            .ToList();
    }
}