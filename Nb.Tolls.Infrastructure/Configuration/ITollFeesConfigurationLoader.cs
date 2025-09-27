using Nb.Tolls.Infrastructure.Models;

namespace Nb.Tolls.Infrastructure.Configuration;

public interface ITollFeesConfigurationLoader
{
    IReadOnlyList<TollFeesModel> LoadFromDataFolder();
}