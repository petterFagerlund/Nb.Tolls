using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Repositories;

public interface ITollFeeRepository
{
    ApplicationResult<TollFeeResult> GetTollFee(DateTime dateTime);
}