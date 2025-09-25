namespace Nb.Tolls.Application.Clients;

public interface INagerHttpClient
{
    Task<bool> IsPublicHolidayAsync(DateOnly date, CancellationToken ct = default);
}