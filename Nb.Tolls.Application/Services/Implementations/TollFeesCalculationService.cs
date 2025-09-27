using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollFeesCalculationService : ITollFeesCalculationService
{
    private readonly ITollFeesRepository _tollFeesRepository;
    private readonly ILogger<TollFeesCalculationService> _logger;

    public TollFeesCalculationService(ITollFeesRepository tollFeesRepository, ILogger<TollFeesCalculationService> logger)
    {
        _tollFeesRepository = tollFeesRepository;
        _logger = logger;
    }

    public ApplicationResult<TollFeesResult> CalculateNonOverlappingTollFees(IReadOnlyList<DateTime> nonOverlappingTollTimes)
    {
        if (nonOverlappingTollTimes.Count == 0)
        {
            _logger.LogError("No non-overlapping toll times provided.");
            return ApplicationResult.WithError<TollFeesResult>("No non-overlapping toll times provided.");
        }

        var ordered = nonOverlappingTollTimes
            .OrderBy(dateTime => dateTime)
            .ToList();

        var fetchResult = GetTollFees(ordered);
        if (!fetchResult.IsSuccessful || fetchResult.Result is null)
        {
            _logger.LogError("Failed to fetch toll fees for non-overlapping times: {@Messages}", [fetchResult.Messages]);
            return ApplicationResult.WithError<TollFeesResult>("Failed to fetch toll fees for non-overlapping times.");
        }

        var tollFees = fetchResult.Result;
        if (tollFees.Count == 0)
        {
            return ApplicationResult.NotFound<TollFeesResult>(
                "No toll fees found for non-overlapping times. Toll times may be outside of toll hours.");
        }

        var tollFeesResult = CalculateDailyTollFeeTotals(tollFees);
        var dailyTollFeeResult = tollFeesResult.TollFees;
        if (dailyTollFeeResult.Count == 0)
        {
            _logger.LogWarning("No toll fees found for non-overlapping times. Toll times may be outside of toll hours.");
            return ApplicationResult.NotFound<TollFeesResult>(
                "No toll fees found for non-overlapping times. Toll times may be outside of toll hours.");
        }

        return ApplicationResult.WithSuccess(new TollFeesResult { TollFees = dailyTollFeeResult });
    }

    public ApplicationResult<TollFeesResult> CalculateOverlappingTollFees(IReadOnlyList<DateTime> overlappingTollTimes)
    {
        if (overlappingTollTimes.Count == 0)
        {
            return ApplicationResult.WithError<TollFeesResult>("No overlapping toll times provided.");
        }

        var orderedTollTimes = overlappingTollTimes
            .OrderBy(dateTime => dateTime)
            .ToList();

        var tollFeeTimesResult = GetTollFees(orderedTollTimes);
        if (!tollFeeTimesResult.IsSuccessful || tollFeeTimesResult.Result is null)
        {
            _logger.LogError("Failed to fetch toll fees for overlapping times: {@Messages}", [tollFeeTimesResult.Messages]);
            return ApplicationResult.WithError<TollFeesResult>("Failed to fetch toll fees for overlapping times.");
        }

        var tollFees = tollFeeTimesResult.Result;
        if (tollFees.Count == 0)
        {
            _logger.LogError(
                "No toll fees found for overlapping times {TollTimes}. Toll times are outside of toll hours.",
                orderedTollTimes);

            return ApplicationResult.NotFound<TollFeesResult>(
                "No toll fees found for overlapping times. Toll times are outside of toll hours.");
        }

        var tollFeesResult = CalculateDailyTollFeeTotals(tollFees);
        var dailyTollFeeResult = tollFeesResult.TollFees;
        if (dailyTollFeeResult.Count == 0)
        {
            _logger.LogWarning("No toll fees found for overlapping times. Toll times may be outside of toll hours.");
            return ApplicationResult.NotFound<TollFeesResult>(
                "No toll fees found for overlapping times. Toll times may be outside of toll hours.");
        }

        return ApplicationResult.WithSuccess(new TollFeesResult { TollFees = dailyTollFeeResult });
    }

    internal TollFeesResult CalculateDailyTollFeeTotals(List<TollTimeFeeResult> tollFees)
    {
        var dailyTollFees = new Dictionary<DateOnly, decimal>();
        foreach (var tollTimeFeeResult in tollFees)
        {
            var tollFee = tollTimeFeeResult.TollFee;
            if (tollFee == 0m)
            {
                continue;
            }

            var day = DateOnly.FromDateTime(tollTimeFeeResult.TollTime.Date);
            if (!dailyTollFees.TryGetValue(day, out var total))
            {
                dailyTollFees[day] = Math.Min(tollFee, 60m);
            }
            else
            {
                dailyTollFees[day] = Math.Min(total + tollFee, 60m);
            }
        }

        var tollFeeResults = dailyTollFees
            .Select(kvp => new DailyTollFeeResult { Date = kvp.Key, TollFee = kvp.Value })
            .OrderBy(d => d.Date)
            .ToList();

        return new TollFeesResult { TollFees = tollFeeResults };
    }

    internal ApplicationResult<List<TollTimeFeeResult>> GetTollFees(List<DateTime> orderedTollTimes)
    {
        if (orderedTollTimes.Count == 0)
        {
            _logger.LogError("No toll times provided to fetch toll fees.");
            return ApplicationResult.WithError<List<TollTimeFeeResult>>("No toll times provided to fetch toll fees.");
        }

        var tollFees = new List<TollTimeFeeResult>();
        foreach (var tollTime in orderedTollTimes)
        {
            var result = _tollFeesRepository.GetTollFee(tollTime);
            if (!result.IsSuccessful || result.Result == null)
            {
                _logger.LogError(
                    "Failed to get toll fee for time {TollTime}: {ErrorMessage}",
                    tollTime,
                    result.Messages);

                return ApplicationResult.WithError<List<TollTimeFeeResult>>(
                    "Internal error occurred while fetching toll fee. Failed to calculate overlapping tolls.");
            }

            tollFees.Add(new TollTimeFeeResult { TollTime = tollTime, TollFee = result.Result.TollFee });
        }

        return ApplicationResult.WithSuccess(tollFees);
    }
}