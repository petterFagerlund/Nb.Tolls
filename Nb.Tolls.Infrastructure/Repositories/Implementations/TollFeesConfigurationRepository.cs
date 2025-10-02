using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Nb.Tolls.Infrastructure.Models;

namespace Nb.Tolls.Infrastructure.Repositories.Implementations;

public class TollFeesConfigurationRepository : ITollFeesConfigurationRepository
{
    private readonly string _filePath;
    private readonly string _folder;

    public TollFeesConfigurationRepository(IConfiguration configuration)
    {
        _filePath = configuration["TollSettings:TollFeesDataPath"] ??
                    throw new NullReferenceException("TollFeesDataPath configuration is missing");
        _folder = configuration["TollSettings:Folder"] ?? throw new NullReferenceException("Folder configuration is missing");
    }

    public IReadOnlyList<TollFeesModel> GetTollFees()
    {
        var basePath = AppContext.BaseDirectory;
        var path = Path.Combine(basePath, _folder, _filePath);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Toll fees configuration file not found.", path);
        }

        return GetTollFeeModels(path);
    }

    internal IReadOnlyList<TollFeesModel> GetTollFeeModels(string path)
    {
        var json = File.ReadAllText(path);
        var tollConfigurationModel =
            JsonSerializer.Deserialize<List<TollFeesModel>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
            throw new InvalidOperationException("Failed to load toll TollFees JSON.");

        return tollConfigurationModel
            .Select(
                tollFeeConfigResults => new TollFeesModel
                {
                    StartMin = tollFeeConfigResults.StartMin,
                    EndMin = tollFeeConfigResults.EndMin,
                    AmountSek = tollFeeConfigResults.AmountSek,
                    Start = tollFeeConfigResults.Start,
                    End = tollFeeConfigResults.End
                })
            .OrderBy(r => r.StartMin)
            .ToList();
    }
}