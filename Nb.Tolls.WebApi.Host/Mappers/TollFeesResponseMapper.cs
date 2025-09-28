using Nb.Tolls.Domain.Results;
using Nb.Tolls.WebApi.Models.Responses;

namespace Nb.Tolls.WebApi.Host.Mappers;

public static class TollFeesResponseMapper
{
    public static TollFeesResponse Map(DailyTollFeesResult dailyTollFeesResult)
    {
        var response = new TollFeesResponse { TollFees = [] };

        foreach (var tollFee in dailyTollFeesResult.TollFees)
        {
            response.TollFees.Add(new TollFeeResponse
            {
                TollDate = tollFee.Date,
                TollFee = tollFee.TollFee
            });
        }

        return response;
    }
}