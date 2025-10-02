using Nb.Tolls.Infrastructure.Models;

namespace Nb.Tolls.Infrastructure.Repositories;

public interface ITollFeesConfigurationRepository
{
    IReadOnlyList<TollFeesModel> GetTollFees();
}