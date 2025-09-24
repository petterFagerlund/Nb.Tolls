using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services;

public interface ITollService
{
    Task<ApplicationResult<TollResult>> GetTollFeeAsync(Vehicle vehicleType, DateTimeOffset[] tollRegistrationTime);
}