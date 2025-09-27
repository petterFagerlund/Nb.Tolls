using Microsoft.Extensions.Logging;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollTimeService : ITollTimeService
{
    private readonly ITollDateService _tollDateService;
    private readonly ILogger<TollTimeService> _logger;

    public TollTimeService(ITollDateService tollDateService, ILogger<TollTimeService> logger)
    {
        _tollDateService = tollDateService;
        _logger = logger;
    }

    public async Task<List<DateTime>> GetEligibleTollFeeTimes(List<DateTime> tollTimes)
    {
        if (tollTimes.Count == 0)
        {
            _logger.LogError("No toll times provided.");
            return [];
        }
        
        var result = new List<DateTime>();
        foreach (var tollTime in tollTimes)
        {
            var isTollFreeDate = await _tollDateService.IsTollFreeDateAsync(tollTime);
            if (isTollFreeDate)
            {
                continue;
            }

            var isTollFreeTime = IsTollFreeTime(tollTime);
            if (isTollFreeTime)
            {
                continue;
            }

            result.Add(tollTime);
        }

        return result;
    }

    public IReadOnlyList<DateTime> GetNonOverlappingTollTimes(List<DateTime> tollTimes)
    {
        if (tollTimes.Count == 0)
        {
            _logger.LogError("No toll times provided.");
            return Array.Empty<DateTime>();
        }

        var orderedTollTimes = tollTimes.OrderBy(dateTime => dateTime).ToList();
        var nonOverlappingTollsTimes = new List<DateTime>();
        DateTime? lastIncludedToll  = null;

        foreach (var currentTollTime in orderedTollTimes)
        {
            if (lastIncludedToll  is null)
            {
                lastIncludedToll  = currentTollTime;
                nonOverlappingTollsTimes.Add(currentTollTime);
            }
            else if (currentTollTime - lastIncludedToll  >= TimeSpan.FromMinutes(60))
            {
                nonOverlappingTollsTimes.Add(currentTollTime);
                lastIncludedToll  = currentTollTime;
            }
        }

        if (nonOverlappingTollsTimes.Count == 1 && orderedTollTimes.Max() - orderedTollTimes.Min() < TimeSpan.FromMinutes(60))
        {
            return Array.Empty<DateTime>();
        }

        return nonOverlappingTollsTimes;
    }

    public IReadOnlyList<DateTime> GetOverlappingTollTimes(List<DateTime> tollTimes)
    {
        if (tollTimes.Count == 0)
        {
            _logger.LogError("No toll times provided.");
            return Array.Empty<DateTime>();
        }
        
        var overlappingTolls = new List<DateTime>();
        DateTime? firstTollInWindow = null;

        foreach (var currentTollTime in tollTimes.OrderBy(dateTime => dateTime))
        {
            if (firstTollInWindow is null)
            {
                firstTollInWindow = currentTollTime;
                continue;
            }

            if (currentTollTime - firstTollInWindow.Value < TimeSpan.FromMinutes(60))
            {
                if (!overlappingTolls.Contains(firstTollInWindow.Value))
                {
                    overlappingTolls.Add(firstTollInWindow.Value);
                }

                overlappingTolls.Add(currentTollTime);
            }
            else
            {
                firstTollInWindow = currentTollTime;
            }
        }

        return overlappingTolls;
    }

    internal static bool IsTollFreeTime(DateTime tollTime)
    {
        var hour = tollTime.Hour;
        var minute = tollTime.Minute;

        if (hour < 6)
        {
            return true;
        }

        if (hour > 18 && minute > 29)
        {
            return true;
        }

        return false;
    }
}