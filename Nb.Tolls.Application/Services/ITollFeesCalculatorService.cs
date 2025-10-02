using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services;

public interface ITollFeesCalculatorService
{
    Task<ApplicationResult<List<TollFeeResult>>> CalculateTollFees(Vehicle vehicleType, DateTimeOffset[] tollTimes);
}