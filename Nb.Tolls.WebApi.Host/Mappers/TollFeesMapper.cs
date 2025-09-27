using Nb.Tolls.Domain.Results;
using Nb.Tolls.WebApi.Models.Responses;

namespace Nb.Tolls.WebApi.Host.Mappers;

public static class TollFeesMapper
{
    public static TollFeesResponse Map(TollFeesResult tollFeesResult)
    {
        var response = new TollFeesResponse { TollFees = [] };

        foreach (var tollFee in tollFeesResult.TollFees)
        {
            response.TollFees.Add(new DailyTollFeeResponse
            {
                Date = tollFee.Date,
                Fee = tollFee.TollFee
            });
        }

        return response;
    }
}