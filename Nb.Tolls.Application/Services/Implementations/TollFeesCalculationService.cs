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

    public ApplicationResult<DailyTollFeesResult> CalculateNonOverlappingTollFees(IReadOnlyList<DateTime> nonOverlappingTollTimes)
    {
        if (nonOverlappingTollTimes.Count == 0)
        {
            _logger.LogError("No non-overlapping toll times provided.");
            return ApplicationResult.WithError<DailyTollFeesResult>("No non-overlapping toll times provided.");
        }

        var ordered = nonOverlappingTollTimes
            .OrderBy(dateTime => dateTime)
            .ToList();

        var fetchResult = GetTollFees(ordered);
        if (!fetchResult.IsSuccessful || fetchResult.Result is null)
        {
            _logger.LogError("Failed to fetch toll fees for non-overlapping times: {@Messages}", [fetchResult.Messages]);
            return ApplicationResult.WithError<DailyTollFeesResult>("Failed to fetch toll fees for non-overlapping times.");
        }

        var tollFees = fetchResult.Result;
        if (tollFees.Count == 0)
        {
            return ApplicationResult.NotFound<DailyTollFeesResult>(
                "No toll fees found for non-overlapping times. Toll times may be outside of toll hours.");
        }

        var tollFeesResult = CalculateTollFees(tollFees);
        var dailyTollFeeResult = tollFeesResult.TollFees;
        if (dailyTollFeeResult.Count == 0)
        {
            _logger.LogWarning("No toll fees found for non-overlapping times. Toll times may be outside of toll hours.");
            return ApplicationResult.NotFound<DailyTollFeesResult>(
                "No toll fees found for non-overlapping times. Toll times may be outside of toll hours.");
        }

        return ApplicationResult.WithSuccess(new DailyTollFeesResult { TollFees = dailyTollFeeResult });
    }

    public ApplicationResult<DailyTollFeesResult> CalculateOverlappingTollFees(IReadOnlyList<DateTime> overlappingTollTimes)
    {
        if (overlappingTollTimes.Count == 0)
        {
            return ApplicationResult.WithError<DailyTollFeesResult>("No overlapping toll times provided.");
        }

        var orderedTollTimes = overlappingTollTimes
            .OrderBy(dateTime => dateTime)
            .ToList();

        var tollFeeTimesResult = GetTollFees(orderedTollTimes);
        if (!tollFeeTimesResult.IsSuccessful || tollFeeTimesResult.Result is null)
        {
            _logger.LogError("Failed to fetch toll fees for overlapping times: {@Messages}", [tollFeeTimesResult.Messages]);
            return ApplicationResult.WithError<DailyTollFeesResult>("Failed to fetch toll fees for overlapping times.");
        }

        var tollFees = tollFeeTimesResult.Result;
        if (tollFees.Count == 0)
        {
            _logger.LogError(
                "No toll fees found for overlapping times {TollTimes}. Toll times are outside of toll hours.",
                orderedTollTimes);

            return ApplicationResult.NotFound<DailyTollFeesResult>(
                "No toll fees found for overlapping times. Toll times are outside of toll hours.");
        }

        var tollFeesResult = CalculateTollFees(tollFees);
        var dailyTollFeeResult = tollFeesResult.TollFees;
        if (dailyTollFeeResult.Count == 0)
        {
            _logger.LogWarning("No toll fees found for overlapping times. Toll times may be outside of toll hours.");
            return ApplicationResult.NotFound<DailyTollFeesResult>(
                "No toll fees found for overlapping times. Toll times may be outside of toll hours.");
        }

        return ApplicationResult.WithSuccess(new DailyTollFeesResult { TollFees = dailyTollFeeResult });
    }

    internal DailyTollFeesResult CalculateTollFees(List<TollFeeResult> tollFees)
    {
        const decimal dailyTollFeeThreshold = 60m;
        var tollsGroupedByDay = tollFees
            .Where(t => t.TollFee > 0)
            .GroupBy(t => DateOnly.FromDateTime(t.TollTime));

        var dailyTollFeeResults = new List<DailyTollFeeResult>();
        foreach (var dailyTollsGroup in tollsGroupedByDay)
        {
            var sortedTollTimeFees = dailyTollsGroup.OrderBy(t => t.TollTime).ToList();
            var dailyTollFees = CalculateDailyTollFees(sortedTollTimeFees);

            var totalDailyTollFee = dailyTollFees.Sum();
            if (totalDailyTollFee > dailyTollFeeThreshold)
            {
                totalDailyTollFee = dailyTollFeeThreshold;
            }

            dailyTollFeeResults.Add(new DailyTollFeeResult { Date = dailyTollsGroup.Key, TollFee = totalDailyTollFee });
        }

        return new DailyTollFeesResult { TollFees = dailyTollFeeResults.OrderBy(d => d.Date).ToList() };
    }

    internal List<decimal> CalculateDailyTollFees(List<TollFeeResult> tollsForASingleDay)
    {
        var tollFeesPerDay = new List<decimal>();
        DateTime? previousTollTimeWindow = null;
        decimal tollTimeWindowHighestToll = 0;

        foreach (var toll in tollsForASingleDay)
        {
            if (previousTollTimeWindow == null)
            {
                previousTollTimeWindow = toll.TollTime;
                tollTimeWindowHighestToll = toll.TollFee;
            }
            else
            {
                var minutesBetweenTollTimeAndPreviousTollTimeWindow = (toll.TollTime - previousTollTimeWindow.Value).TotalMinutes;
                if (minutesBetweenTollTimeAndPreviousTollTimeWindow >= 60)
                {
                    tollFeesPerDay.Add(tollTimeWindowHighestToll);
                    previousTollTimeWindow = toll.TollTime;
                    tollTimeWindowHighestToll = toll.TollFee;
                }
                else
                {
                    tollTimeWindowHighestToll = Math.Max(tollTimeWindowHighestToll, toll.TollFee);
                }
            }
        }

        if (previousTollTimeWindow != null)
        {
            tollFeesPerDay.Add(tollTimeWindowHighestToll);
        }

        return tollFeesPerDay;
    }

    internal ApplicationResult<List<TollFeeResult>> GetTollFees(List<DateTime> orderedTollTimes)
    {
        if (orderedTollTimes.Count == 0)
        {
            _logger.LogError("No toll times provided to fetch toll fees.");
            return ApplicationResult.WithError<List<TollFeeResult>>("No toll times provided to fetch toll fees.");
        }

        var result = _tollFeesRepository.GetTollFees(orderedTollTimes);

        if (!result.IsSuccessful || result.Result == null || result.Result.Count != orderedTollTimes.Count)
        {
            _logger.LogError(
                "Failed to get toll fees for the batch: {ErrorMessage}",
                [result.Messages]);

            return ApplicationResult.WithError<List<TollFeeResult>>(
                "Internal error occurred while fetching toll fees.");
        }

        return ApplicationResult.WithSuccess(result.Result);
    }
}