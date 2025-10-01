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

    public List<DateTime> GetNonOverlappingTollTimes(List<DateTime> tollTimes)
    {
        tollTimes.Sort();
        var nonOverlappingTollTimes = new List<DateTime>();
        DateTime? lastTollTime = null;

        foreach (var currentTollTime in tollTimes)
        {
            if (lastTollTime is null)
            {
                lastTollTime = currentTollTime;
                nonOverlappingTollTimes.Add(currentTollTime);
            }
            else if (currentTollTime - lastTollTime >= TimeSpan.FromMinutes(60))
            {
                nonOverlappingTollTimes.Add(currentTollTime);
                lastTollTime = currentTollTime;
            }
        }

        if (nonOverlappingTollTimes.Count == 0 || 
            nonOverlappingTollTimes.Count == 1 && tollTimes.Max() - tollTimes.Min() < TimeSpan.FromMinutes(60))
        {
            return [];
        }

        return nonOverlappingTollTimes;
    }

    public List<DateTime> GetOverlappingTollTimes(List<DateTime> tollTimes)
    {
        var overlappingTolls = new List<DateTime>();
        DateTime? lastTollTime = null;
        tollTimes.Sort();

        foreach (var currentTollTime in tollTimes)
        {
            if (lastTollTime is null)
            {
                lastTollTime = currentTollTime;
                continue;
            }

            if (currentTollTime - lastTollTime.Value < TimeSpan.FromMinutes(60))
            {
                if (!overlappingTolls.Contains(lastTollTime.Value))
                {
                    overlappingTolls.Add(lastTollTime.Value);
                }

                overlappingTolls.Add(currentTollTime);
            }
            else
            {
                lastTollTime = currentTollTime;
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