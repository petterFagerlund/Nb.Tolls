using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Domain.Results;
using Nb.Tolls.Infrastructure.Models;

namespace Nb.Tolls.Infrastructure.Repositories.Implementations;

public class TollFeesRepository : ITollFeesRepository
{
    private readonly ILogger<TollFeesRepository> _logger;
    private readonly IReadOnlyList<TollFeesModel> _tollFees;
    private readonly string _timeZone;

    public TollFeesRepository(
        ITollFeesConfigurationRepository tollFeesConfigurationRepository,
        IConfiguration configuration,
        ILogger<TollFeesRepository> logger)
    {
        _logger = logger;
        _tollFees = tollFeesConfigurationRepository.GetTollFees();
        _timeZone = configuration["TollSettings:TimeZone"] ??
                    throw new NullReferenceException("Timezone configuration is missing");
    }

    public ApplicationResult<List<TollFeeResult>> GetTollFees(IEnumerable<DateTime> dateTimes)
    {
        var results = new List<TollFeeResult>();
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_timeZone);
            foreach (var dateTime in dateTimes)
            {
                var utcTime = dateTime.Kind == DateTimeKind.Utc
                    ? dateTime
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

                var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
                var minuteOfDay = localDateTime.Hour * 60 + localDateTime.Minute;

                var tollFeesModel = _tollFees.FirstOrDefault(
                    tollFeesModel => minuteOfDay >= tollFeesModel.StartMin && minuteOfDay < tollFeesModel.EndMin);
                if (tollFeesModel == null)
                {
                    _logger.LogWarning("No toll fee rule found for time: {DateTime}", localDateTime);
                    return ApplicationResult.NotFound<List<TollFeeResult>>(
                        "No toll fee rule found for time: " + localDateTime);
                }

                results.Add(new TollFeeResult { TollFeeTime = localDateTime, TollFee = tollFeesModel.AmountSek });
            }

            if (results.Count == 0)
            {
                _logger.LogWarning("No toll fees found for the provided times.");
                return ApplicationResult.NotFound<List<TollFeeResult>>("No toll fees found for the provided times.");
            }

            return ApplicationResult.WithSuccess(results);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in CalculateTollFees batch: {Message}", e.Message);
            return ApplicationResult.WithError<List<TollFeeResult>>("Error calculating toll fees: " + e.Message);
        }
    }
}