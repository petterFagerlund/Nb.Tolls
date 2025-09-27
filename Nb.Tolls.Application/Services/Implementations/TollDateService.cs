using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Clients;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollDateService : ITollDateService
{
    private readonly INagerHttpClient _nagerHttpClient;
    private readonly ILogger<TollDateService> _logger;

    public TollDateService(ILogger<TollDateService> logger, INagerHttpClient nagerHttpClient)
    {
        _logger = logger;
        _nagerHttpClient = nagerHttpClient;
    }
    
    public async Task<bool> IsTollFreeDateAsync(DateTime timestamp)
    {
        var date = DateOnly.FromDateTime(timestamp);
        return await IsTollFreeCalendarDateAsync(date);
    }

    internal async Task<bool> IsTollFreeCalendarDateAsync(DateOnly date)
    {
        if (date.Month == 7)
        {
            return true;
        }

        if (date.DayOfWeek == DayOfWeek.Saturday)
        {
            return true;
        }

        if (await IsPublicHolidayOrSundayAsync(date))
        {
            return true;
        }

        if (await IsPublicHolidayOrSundayAsync(date.AddDays(1)))
        {
            return true;
        }

        return false;
    }

    internal async Task<bool> IsPublicHolidayOrSundayAsync(DateOnly date)
    {
        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            return true;
        }

        try
        {
            return await _nagerHttpClient.IsPublicHolidayAsync(date);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check holidays for {Date}. Treating as non-holiday.", date);
            return false;
        }
    }
}