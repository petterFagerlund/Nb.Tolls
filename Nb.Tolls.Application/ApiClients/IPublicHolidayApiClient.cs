namespace Nb.Tolls.Application.ApiClients;

public interface IPublicHolidayApiClient
{
    Task<bool> IsPublicHolidayAsync(DateOnly date, CancellationToken ct = default);
}