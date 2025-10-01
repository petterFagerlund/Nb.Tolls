using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.ApiClients;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollDateService : ITollDateService
{
    private readonly IPublicHolidayApiClient _publicHolidayApiClient;
    private readonly ILogger<TollDateService> _logger;

    public TollDateService(ILogger<TollDateService> logger, IPublicHolidayApiClient publicHolidayApiClient)
    {
        _logger = logger;
        _publicHolidayApiClient = publicHolidayApiClient;
    }
    
    public async Task<bool> IsTollFreeDate(DateTime timestamp)
    {
        var date = DateOnly.FromDateTime(timestamp);
        return await IsTollFreeCalendarDate(date);
    }

    internal async Task<bool> IsTollFreeCalendarDate(DateOnly date)
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
            return await _publicHolidayApiClient.IsPublicHolidayAsync(date);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check holidays for {TollFeeDate}. Treating as non-holiday.", date);
            return false;
        }
    }
}