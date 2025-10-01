using Nb.Tolls.Domain.Results;
using Nb.Tolls.WebApi.Models.Responses;

namespace Nb.Tolls.WebApi.Host.Mappers;

public static class TollFeesResponseMapper
{
    public static TollFeesResponse Map(DailyTollFeesResult dailyTollFeesResult)
    {
        var response = new TollFeesResponse { TollFees = [] };
        if (dailyTollFeesResult.TollFees == null)
        {
            return response;
        }

        foreach (var tollFee in dailyTollFeesResult.TollFees)
        {
            response.TollFees.Add(new TollFeeResponse
            {
                TollDate = DateOnly.FromDateTime(tollFee.TollFeeTime),
                TollFee = tollFee.TollFee
            });
        }

        return response;
    }
}