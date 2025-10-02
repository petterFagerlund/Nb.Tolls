using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Helpers;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollFeesCalculatorService : ITollFeesCalculatorService
{
    private readonly ITollFeesRepository _tollFeesRepository;
    private readonly ITollTimeService _tollTimeService;
    private readonly ILogger<TollFeesCalculatorService> _logger;

    public TollFeesCalculatorService(
        ITollFeesRepository tollFeesRepository,
        ITollTimeService tollTimeService,
        ILogger<TollFeesCalculatorService> logger)
    {
        _tollFeesRepository = tollFeesRepository;
        _tollTimeService = tollTimeService;
        _logger = logger;
    }

    public async Task<ApplicationResult<List<TollFeeResult>>> CalculateTollFees(
        Vehicle vehicleType,
        DateTimeOffset[] tollTimes)
    {
        if (VehicleTollPolicyHelper.IsTollFreeVehicle(vehicleType))
        {
            return ApplicationResult.NotFound<List<TollFeeResult>>("Vehicle type is toll-free.");
        }

        var tollTimesUtc = tollTimes
            .Select(dto => dto.UtcDateTime)
            .ToList();

        var eligibleTollFeeTimes = await _tollTimeService.GetEligibleTollFeeTimes(tollTimesUtc);
        if (eligibleTollFeeTimes.Count == 0)
        {
            _logger.LogInformation(
                "No eligible toll fee times found after filtering: {@TollFeeTimes}",
                eligibleTollFeeTimes);
            return ApplicationResult.NotFound<List<TollFeeResult>>("No eligible toll fee times found after filtering.");
        }

        var result = new List<TollFeeResult>();

        var nonOverlappingTollTimes = _tollTimeService.GetNonOverlappingTollTimes(eligibleTollFeeTimes);
        if (nonOverlappingTollTimes.Count != 0)
        {
            var nonOverlappingTollFeesResult = CalculateTollFeesFromValidTollFeeTimes(nonOverlappingTollTimes);
            if (nonOverlappingTollFeesResult.IsSuccessful)
            {
                result.AddRange(nonOverlappingTollFeesResult.Result);
            }
        }

        var overlappingTollTimes = _tollTimeService.GetOverlappingTollTimes(eligibleTollFeeTimes);
        if (overlappingTollTimes.Count != 0)
        {
            var overlappingTollFeesResult = CalculateTollFeesFromValidTollFeeTimes(overlappingTollTimes);
            if (overlappingTollFeesResult.IsSuccessful)
            {
                result.AddRange(overlappingTollFeesResult.Result);
            }
        }

        if (result.Count == 0)
        {
            _logger.LogWarning("No toll fees found for provided times {@TollFeeTimes}", eligibleTollFeeTimes);
            return ApplicationResult.NotFound<List<TollFeeResult>>(
                "No toll fees found for provided times.");
        }

        return ApplicationResult.WithSuccess(result);
    }

    internal ApplicationResult<List<TollFeeResult>> CalculateTollFeesFromValidTollFeeTimes(
        IEnumerable<DateTime> tollTimes)
    {
        var tollFeesResult = _tollFeesRepository.GetTollFees(tollTimes);
        if (!tollFeesResult.IsSuccessful)
        {
            _logger.LogError("Failed to fetch toll fees for overlapping times: {@Messages}", [tollFeesResult.Messages]);
            return ApplicationResult.WithError<List<TollFeeResult>>("Failed to fetch toll fees for overlapping times.");
        }

        const int dailyTollFeeThreshold = 60;
        var tollFeesGroupedByDay = tollFeesResult.Result
            .GroupBy(t => DateOnly.FromDateTime(t.TollFeeTime));

        var dailyTollFeeResults = new List<TollFeeResult>();
        foreach (var tollFees in tollFeesGroupedByDay)
        {
            var tollFeeDate = tollFees.Key;
            var dailyTollFees = CalculateDailyTollFees(tollFees.ToList());
            var totalDailyTollFee = dailyTollFees.Sum();

            if (totalDailyTollFee > dailyTollFeeThreshold)
            {
                totalDailyTollFee = dailyTollFeeThreshold;
            }

            dailyTollFeeResults.Add(
                new TollFeeResult { TollFeeTime = tollFeeDate.ToDateTime(TimeOnly.MinValue), TollFee = totalDailyTollFee });
        }

        if (dailyTollFeeResults.Count == 0)
        {
            _logger.LogWarning("No toll fees found for overlapping times. Toll times may be outside of toll hours.");
            return ApplicationResult.NotFound<List<TollFeeResult>>(
                "No toll fees found for overlapping times. Toll times may be outside of toll hours.");
        }

        return ApplicationResult.WithSuccess(dailyTollFeeResults);
    }

    internal List<int> CalculateDailyTollFees(IEnumerable<TollFeeResult> tollFeeResults)
    {
        var tollFeesPerDay = new List<int>();
        DateTime? previousTollTime = null;
        var previousTollFee = 0;
        var tollFeesByTollTime = tollFeeResults.OrderBy(t => t.TollFeeTime).ToList();


        foreach (var tollFeeResult in tollFeesByTollTime)
        {
            if (previousTollTime == null)
            {
                previousTollTime = tollFeeResult.TollFeeTime;
                previousTollFee = tollFeeResult.TollFee;
                continue;
            }

            var minutesBetweenTollTimeAndPreviousTollTimeWindow =
                (tollFeeResult.TollFeeTime - previousTollTime.Value).TotalMinutes;
            if (minutesBetweenTollTimeAndPreviousTollTimeWindow >= 60)
            {
                tollFeesPerDay.Add(previousTollFee);
                previousTollTime = tollFeeResult.TollFeeTime;
                previousTollFee = tollFeeResult.TollFee;
            }
            else
            {
                previousTollFee = Math.Max(previousTollFee, tollFeeResult.TollFee);
            }
        }

        if (previousTollTime != null)
        {
            tollFeesPerDay.Add(previousTollFee);
        }

        return tollFeesPerDay;
    }
}