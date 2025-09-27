using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Helpers;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollFeesService : ITollFeesService
{
    private readonly ITollFeesCalculationService _tollFeesCalculationService;
    private readonly ITollTimeService _tollTimeService;
    private readonly ILogger<TollFeesService> _logger;

    public TollFeesService(
        ITollFeesCalculationService tollFeesCalculationService,
        ITollTimeService tollTimeService,
        ILogger<TollFeesService> logger)
    {
        _tollFeesCalculationService = tollFeesCalculationService;
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

            if (tollTimes.Length == 0)
            {
                _logger.LogInformation("No toll times provided.");
                return ApplicationResult.NotFound<TollFeesResult>("No toll times provided.");
            }

            //todo: consider timezone handling throughout the application
            var tollTimesUtc = tollTimes
                .Select(dateTimeOffset => dateTimeOffset.UtcDateTime)
                .ToList();

            var eligibleTollFeeTimes = await _tollTimeService.GetEligibleTollFeeTimes(tollTimesUtc);
            if (eligibleTollFeeTimes.Count == 0)
            {
                _logger.LogInformation("No eligible toll fee times found after filtering.");
                return ApplicationResult.WithSuccess(new TollFeesResult { TollFees = [] });
            }

            var nonOverlappingTollTimes = _tollTimeService.GetNonOverlappingTollTimes(eligibleTollFeeTimes);
            var nonOverlappingTollFeesResult = new ApplicationResult<TollFeesResult>();
            if (nonOverlappingTollTimes.Count != 0)
            {
                nonOverlappingTollFeesResult =
                    _tollFeesCalculationService.CalculateNonOverlappingTollFees(nonOverlappingTollTimes);
                if (!nonOverlappingTollFeesResult.IsSuccessful || nonOverlappingTollFeesResult.Result == null)
                {
                    _logger.LogError(
                        "Failed to calculate non-overlapping toll fees: {@Messages}",
                        [nonOverlappingTollFeesResult.Messages]);
                    return ApplicationResult.WithError<TollFeesResult>("Failed to calculate toll fees.");
                }
            }

            var overlappingTollTimes = _tollTimeService.GetOverlappingTollTimes(eligibleTollFeeTimes);
            if (overlappingTollTimes.Count == 0 && nonOverlappingTollFeesResult.Result != null)
            {
                return ApplicationResult.WithSuccess(nonOverlappingTollFeesResult.Result);
            }

            var overlappingTollFeesResult = _tollFeesCalculationService.CalculateOverlappingTollFees(overlappingTollTimes);
            if (!overlappingTollFeesResult.IsSuccessful || overlappingTollFeesResult.Result == null)
            {
                _logger.LogError(
                    "Failed to calculate non-overlapping toll fees: {@Messages}",
                    [nonOverlappingTollFeesResult.Messages]);
                return ApplicationResult.WithError<TollFeesResult>("Failed to calculate toll fees.");
            }

            var result = new TollFeesResult { TollFees = [] };
            var nonOverlappingTollFees = nonOverlappingTollFeesResult.Result?.TollFees ?? [];
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
}