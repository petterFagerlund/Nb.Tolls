using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services;

public interface ITollFeesCalculatorService
{
    Task<ApplicationResult<DailyTollFeesResult>> CalculateTollFees(Vehicle vehicleType, DateTimeOffset[] tollTimes);
}