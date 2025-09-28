using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Repositories;

public interface ITollFeesRepository
{
    ApplicationResult<List<TollFeeResult>> GetTollFees(IEnumerable<DateTime> dateTimes);
}