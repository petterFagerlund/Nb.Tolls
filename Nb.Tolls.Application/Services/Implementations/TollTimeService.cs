using Microsoft.Extensions.Logging;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollTimeService : ITollTimeService
{
    private readonly ITollDateService _tollDateService;
    private readonly ILogger<TollTimeService> _logger;

    public TollTimeService(ITollDateService tollDateService,ILogger<TollTimeService> logger)
    {
        _tollDateService = tollDateService;
        _logger = logger;
    }

    public async Task<List<DateTimeOffset>> ExtractEligibleTollFeeTimes(List<DateTimeOffset> tollTimes)
    {
        var result = new List<DateTimeOffset>();
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

    public IReadOnlyList<DateTimeOffset> ExtractNonOverlappingTollTimes(List<DateTimeOffset> tollTimes)
    {
        var result = new List<DateTimeOffset>();
        DateTimeOffset? lastKept = null;

        foreach (var tollTime in tollTimes)
        {
            if (lastKept is null || tollTime - lastKept.Value >= TimeSpan.FromMinutes(60))
            {
                result.Add(tollTime);
                lastKept = tollTime;
            }
        }

        return result;
    }

    public IReadOnlyList<DateTimeOffset> ExtractOverlappingTollTimes(List<DateTimeOffset> tollTimes)
    {
        var result = new List<DateTimeOffset>();
        DateTimeOffset? anchor = null;

        foreach (var tollTime in tollTimes)
        {
            if (anchor is null)
            {
                anchor = tollTime;
                continue;
            }

            if (tollTime - anchor.Value < TimeSpan.FromMinutes(60))
            {
                result.Add(tollTime);
            }
            else
            {
                anchor = tollTime;
            }
        }

        return result;
    }
    
    internal bool IsTollFreeTime(DateTimeOffset tollTime)
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