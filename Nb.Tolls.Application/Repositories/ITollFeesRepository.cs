using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Repositories;

public interface ITollFeesRepository
{
    ApplicationResult<TollFeeResult> GetTollFee(DateTime dateTime);
}