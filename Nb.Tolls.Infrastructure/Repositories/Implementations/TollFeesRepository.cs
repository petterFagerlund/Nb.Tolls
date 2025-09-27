using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Domain.Results;
using Nb.Tolls.Infrastructure.Configuration;
using Nb.Tolls.Infrastructure.Models;

namespace Nb.Tolls.Infrastructure.Repositories.Implementations;

public class TollFeesRepository : ITollFeesRepository
{
    private readonly ILogger<TollFeesRepository> _logger;
    private readonly IReadOnlyList<TollFeesModel> _tollFees;
    private readonly string _timeZone;

    public TollFeesRepository(
        ITollFeesConfigurationLoader tollFeesConfigurationLoader,
        IConfiguration configuration,
        ILogger<TollFeesRepository> logger)
    {
        _logger = logger;
        _tollFees = tollFeesConfigurationLoader.LoadFromDataFolder();
        _timeZone = configuration["TollSettings:TimeZone"] ??
                    throw new NullReferenceException("Timezone configuration is missing");
    }

    public ApplicationResult<TollFeeResult> GetTollFee(DateTime dateTime)
    {
        try
        {
            var stockholmTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_timeZone);
            var localDateTime = TimeZoneInfo.ConvertTime(dateTime, stockholmTimeZone);
            var minuteOfDay = localDateTime.Hour * 60 + localDateTime.Minute;

            var rule = _tollFees.FirstOrDefault(
                rule =>
                    minuteOfDay >= rule.StartMin &&
                    minuteOfDay < rule.EndMin);

            if (rule == null)
            {
                _logger.LogError("No toll fee rule found for time: {DateTime}", localDateTime);
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
}