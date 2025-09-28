using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services;

public interface ITollFeesService
{
    Task<ApplicationResult<DailyTollFeesResult>> GetTollFees(Vehicle vehicleType, DateTimeOffset[] tollTimes);
}