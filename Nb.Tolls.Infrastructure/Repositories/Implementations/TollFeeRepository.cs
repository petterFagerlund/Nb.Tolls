using System.Data;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Repositories;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Infrastructure.Repositories.Implementations;

public class TollFeeRepository : ITollFeeRepository
{
    private readonly ILogger<TollFeeRepository> _logger;
    private readonly IReadOnlyList<TollFeeConfigResult> _tollFees;

    public TollFeeRepository(IHostEnvironment env, ILogger<TollFeeRepository> logger)
    {
        _logger = logger;
        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var path = Path.Combine(assemblyFolder, "Data", "TollFees.json");

        if (!File.Exists(path))
        {
            throw new NullReferenceException("Toll fees configuration file not found.");
        }

        _tollFees = LoadRules(path);
    }

    public ApplicationResult<TollFeeResult> GetTollFee(DateTime dateTime)
    {
        try
        {
            var minuteOfDay = dateTime.Hour * 60 + dateTime.Minute;
            var rule = _tollFees.FirstOrDefault(rule => minuteOfDay >= rule.StartMin && minuteOfDay < rule.EndMin);
            if (rule == null)
            {
                _logger.LogError("No toll fee rule found for time: {DateTime}", dateTime);
                return ApplicationResult.WithNotFound(new TollFeeResult { TollFee = 0 });
            }

            return ApplicationResult.WithSuccess(new TollFeeResult { TollFee = rule.AmountSek });
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in GetTollFees: {Message}", e.Message);
            return ApplicationResult.WithError<TollFeeResult>("Error calculating toll fee: " + e.Message);
        }
    }

    private static List<TollFeeConfigResult> LoadRules(string path)
    {
        var json = File.ReadAllText(path);
        var tollConfig =
            JsonSerializer.Deserialize<TollFeeConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
            throw new InvalidOperationException("Failed to load toll TollConfigResult JSON.");

        return tollConfig.TollConfigResult
            .Select(
                tollFeeConfigResults => new TollFeeConfigResult
                {
                    StartMin = tollFeeConfigResults.StartMin ?? ParseMinutes(tollFeeConfigResults.Start),
                    EndMin = tollFeeConfigResults.EndMin ?? ParseMinutes(tollFeeConfigResults.End),
                    AmountSek = tollFeeConfigResults.AmountSek
                })
            .OrderBy(r => r.StartMin)
            .ToList();
    }

    private static int ParseMinutes(string hhmm)
    {
        if (hhmm == "24:00")
        {
            return 24 * 60;
        }

        var timeSpan = TimeSpan.ParseExact(hhmm, "hh\\:mm", CultureInfo.InvariantCulture);
        return (int)timeSpan.TotalMinutes;
    }
}

internal class TollFeeConfig
{
    public string? Timezone { get; set; }
    public string? Semantics { get; set; }
    public required List<TollFeeConfigResult> TollConfigResult { get; set; }
}

internal class TollFeeConfigResult
{
    public string Start { get; set; } = default!;
    public string End { get; set; } = default!;
    public decimal AmountSek { get; set; }
    public int? StartMin { get; set; }
    public int? EndMin { get; set; }
}