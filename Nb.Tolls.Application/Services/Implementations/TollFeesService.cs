using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Helpers;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollFeesService : ITollFeesService
{
    private readonly ITollFeeRepository _tollFeeRepository;
    private readonly ITollTimeService _tollTimeService;
    private readonly ILogger<TollFeesService> _logger;

    public TollFeesService(
        ITollFeeRepository tollFeeRepository,
        ITollTimeService tollTimeService,
        ILogger<TollFeesService> logger)
    {
        _tollFeeRepository = tollFeeRepository;
        _tollTimeService = tollTimeService;
        _logger = logger;
    }

    public async Task<ApplicationResult<TollFeesResult>> GetTollFees(
        Vehicle vehicleType,
        DateTimeOffset[] tollTimes)
    {
        try
        {
            if (TollServiceHelper.IsTollFreeVehicle(vehicleType))
            {
                _logger.LogInformation("Vehicle type {VehicleType} is toll-free.", vehicleType);
                return ApplicationResult.WithSuccess(new TollFeesResult { TollFees = [] });
            }

            var tollTimesUtc = tollTimes
                .OrderBy(t => t.UtcDateTime)
                .ToList();

            var eligibleTollFeeTimes = await _tollTimeService.ExtractEligibleTollFeeTimes(tollTimesUtc);
            var nonOverlappingTollTimes = _tollTimeService.ExtractNonOverlappingTollTimes(eligibleTollFeeTimes);
            var nonOverlappingTollFeesResult = CalculateNonOverlappingTollFees(nonOverlappingTollTimes);

            if (!nonOverlappingTollFeesResult.IsSuccessful || nonOverlappingTollFeesResult.Result == null)
            {
                _logger.LogError(
                    "Failed to calculate non-overlapping toll fees: {@Messages}",
                    [nonOverlappingTollFeesResult.Messages]);
                return ApplicationResult.WithError<TollFeesResult>("Failed to calculate toll fees.");
            }

            var overlappingTollTimes = _tollTimeService.ExtractOverlappingTollTimes(eligibleTollFeeTimes);
            var overlappingTollFeesResult = CalculateOverlappingTollFees(overlappingTollTimes);
            if (!overlappingTollFeesResult.IsSuccessful || overlappingTollFeesResult.Result == null)
            {
                _logger.LogError(
                    "Failed to calculate non-overlapping toll fees: {@Messages}",
                    [nonOverlappingTollFeesResult.Messages]);
                return ApplicationResult.WithError<TollFeesResult>("Failed to calculate toll fees.");
            }

            var result = new TollFeesResult { TollFees = [] };
            var nonOverlappingTollFees = nonOverlappingTollFeesResult.Result.TollFees;
            var overlappingTollFees = overlappingTollFeesResult.Result.TollFees;

            if (nonOverlappingTollFees.Count == 0 && overlappingTollFees.Count == 0)
            {
                _logger.LogError("No toll fees found for provided times.");
                return ApplicationResult.NotFound<TollFeesResult>(
                    "No toll fees found for the provided times. Times may be outside of toll hours.");
            }

            if (nonOverlappingTollFees.Count > 0)
            {
                result.TollFees.AddRange(nonOverlappingTollFees);
            }

            if (overlappingTollFees.Count > 0)
            {
                result.TollFees.AddRange(overlappingTollFees);
            }

            return ApplicationResult.WithSuccess(result);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in GetTollFees: {Message}", e.Message);
            return ApplicationResult.WithError<TollFeesResult>("Internal error occurred while fetching tolls.");
        }
    }

    internal ApplicationResult<TollFeesResult> CalculateNonOverlappingTollFees(
        IReadOnlyList<DateTimeOffset> nonOverlappingTollTimes)
    {
        if (nonOverlappingTollTimes.Count == 0)
        {
            _logger.LogError("No non-overlapping toll times provided.");
            return ApplicationResult.WithError<TollFeesResult>("No non-overlapping toll times provided.");
        }

        var ordered = nonOverlappingTollTimes
            .OrderBy(t => t.UtcDateTime)
            .ToList();

        var fetchResult = FetchTollFees(ordered);
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

        var dailyTotals = new Dictionary<DateOnly, decimal>();
        foreach (var tollTimeFeeResult in tollFees)
        {
            var tollFee = tollTimeFeeResult.TollFee;
            if (tollFee == 0m)
            {
                continue;
            }


            var day = DateOnly.FromDateTime(tollTimeFeeResult.TollTime.Date);
            if (!dailyTotals.TryGetValue(day, out var total))
            {
                dailyTotals[day] = Math.Min(tollFee, 60m);
            }
            else
            {
                dailyTotals[day] = Math.Min(total + tollFee, 60m);
            }
        }

        if (dailyTotals.Count == 0)
        {
            _logger.LogWarning("No toll fees found for non-overlapping times. Toll times may be outside of toll hours.");
            return ApplicationResult.NotFound<TollFeesResult>(
                "No toll fees found for non-overlapping times. Toll times may be outside of toll hours.");
        }

        var dailyList = dailyTotals
            .Select(kvp => new DailyTollFeeResult { Date = kvp.Key, Fee = kvp.Value })
            .OrderBy(x => x.Date)
            .ToList();

        return ApplicationResult.WithSuccess(new TollFeesResult { TollFees = dailyList });
    }
    
    internal ApplicationResult<TollFeesResult> CalculateOverlappingTollFees(IReadOnlyList<DateTimeOffset> overlappingTollTimes)
    {
        if (overlappingTollTimes.Count == 0)
        {
            return ApplicationResult.WithError<TollFeesResult>("No overlapping toll times provided.");
        }

        var orderedTollTimes = overlappingTollTimes
            .OrderBy(t => t.UtcDateTime)
            .ToList();

        var tollFeeTimesResult = FetchTollFees(orderedTollTimes);
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

        var dailyTollFeeResults = new List<DailyTollFeeResult>();
        var tollFeesGroupedByDay = tollFees.GroupBy(tf => DateOnly.FromDateTime(tf.TollTime.Date));

        foreach (var dayGroup in tollFeesGroupedByDay)
        {
            var fees = dayGroup.OrderBy(tf => tf.TollTime).ToList();
            var windowStart = fees[0].TollTime;
            var currentWindowMax = fees[0].TollFee;
            var dayTotal = 0m;

            foreach (var tollTimeFeeResult in fees.Skip(1))
            {
                var tollTime = tollTimeFeeResult.TollTime;
                var tollFee = tollTimeFeeResult.TollFee;

                if (tollTime - windowStart < TimeSpan.FromMinutes(60))
                {
                    if (tollFee > currentWindowMax)
                    {
                        currentWindowMax = tollFee;
                    }
                }
                else
                {
                    dayTotal += currentWindowMax;

                    windowStart = tollTime;
                    currentWindowMax = tollFee;
                }
            }

            dayTotal += currentWindowMax;

            dailyTollFeeResults.Add(new DailyTollFeeResult { Date = dayGroup.Key, Fee = Math.Min(dayTotal, 60m) });
        }

        return ApplicationResult.WithSuccess(new TollFeesResult { TollFees = dailyTollFeeResults });
    }

    internal ApplicationResult<List<TollTimeFeeResult>> FetchTollFees(List<DateTimeOffset> orderedTollTimes)
    {
        if (orderedTollTimes.Count == 0)
        {
            _logger.LogError("No toll times provided to fetch toll fees.");
            return ApplicationResult.WithError<List<TollTimeFeeResult>>("No toll times provided to fetch toll fees.");
        }

        var tollFees = new List<TollTimeFeeResult>();
        foreach (var tollTime in orderedTollTimes)
        {
            var result = _tollFeeRepository.GetTollFee(tollTime.UtcDateTime);
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