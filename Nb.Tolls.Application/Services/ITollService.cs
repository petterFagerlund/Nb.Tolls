using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services;

public interface ITollService
{
    Task<ApplicationResult<TollResult>> GetTollAsync();
}