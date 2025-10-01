using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nb.Tolls.Application.ApiClients;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Infrastructure.ApiClients;
using Nb.Tolls.Infrastructure.Configuration;
using Nb.Tolls.Infrastructure.Repositories.Implementations;

namespace Nb.Tolls.Infrastructure.Registrations;

public static class ServiceCollectionsExtension
{
    public static IServiceCollection AddTollsInfrastructure(
        this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddMemoryCache();

        var nagerBaseUrl = configuration["Nager:BaseUrl"];
        if (string.IsNullOrEmpty(nagerBaseUrl))
        {
            throw new ArgumentNullException(nameof(nagerBaseUrl), "Nager:BaseUrl is missing in configuration");
        }

        services.AddHttpClient<IPublicHolidayApiClient, PublicHolidayApiClient>(
            client =>
            {
                client.BaseAddress = new Uri(nagerBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
            });
        services.AddTransient<ITollFeesRepository, TollFeesRepository>();
        services.AddTransient<ITollFeesConfigurationLoader, TollFeesConfigurationLoader>();
        return services;
    }
}