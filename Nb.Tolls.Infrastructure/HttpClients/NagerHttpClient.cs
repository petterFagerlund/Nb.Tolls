using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Clients;

namespace Nb.Tolls.Infrastructure.HttpClients;

public class NagerHttpClient : INagerHttpClient
{
    private readonly ILogger<NagerHttpClient> _logger;
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public NagerHttpClient(ILogger<NagerHttpClient> logger, HttpClient http, IMemoryCache cache)
    {
        _logger = logger;
        _http = http;
        _cache = cache;
    }

    
    //Todo: Clean up and add better logging 
    public async Task<bool> IsPublicHolidayAsync(DateOnly date, CancellationToken ct = default)
    {
        var year = date.Year;
        var holidays = await GetHolidaysForYearAsync(year, ct).ConfigureAwait(false);
        return holidays.Contains(date.ToString("yyyy-MM-dd"));
    }

    private async Task<HashSet<string>> GetHolidaysForYearAsync(int year, CancellationToken ct)
    {
        var cacheKey = $"nager:SE:{year}";
        if (_cache.TryGetValue(cacheKey, out HashSet<string> cached))
            return cached;

        var url = $"/api/v3/PublicHolidays/{year}/SE";
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var empty = new HashSet<string>();
            _cache.Set(cacheKey, empty, TimeSpan.FromMinutes(1));
            return empty;
        }

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var set = new HashSet<string>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.TryGetProperty("date", out var dateProp))
            {
                set.Add(dateProp.GetString()!);
            }
        }

        _cache.Set(cacheKey, set, TimeSpan.FromDays(30));
        return set;
    }
}