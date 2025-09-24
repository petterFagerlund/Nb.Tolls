using Microsoft.Extensions.Logging;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollService : ITollService
{
    private readonly ILogger<TollService> _logger;

    public TollService(ILogger<TollService> logger)
    {
        _logger = logger;
    }
    
    public async Task<ApplicationResult<TollResult>> GetTollAsync()
    {
        try
        {
            throw new NotImplementedException();

        }
        catch (Exception e)
        {
            _logger.LogError("Exception in GetTollAsync: {Message}", e.Message);
            return ApplicationResult.WithError<TollResult>("Internal error occurred while fetching tolls.");
        }
    }
}