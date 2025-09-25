using Microsoft.Extensions.DependencyInjection;
using Nb.Tolls.Application.Clients;
using Nb.Tolls.Application.Repositories;
using Nb.Tolls.Infrastructure.HttpClients;
using Nb.Tolls.Infrastructure.Repositories.Implementations;

namespace Nb.Tolls.Infrastructure.Registrations;

public static class ServiceCollectionsExtension
{
    public static IServiceCollection AddTollsInfrastructure(
        this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<INagerHttpClient, NagerHttpClient>(client =>
        {
            client.BaseAddress = new Uri("https://date.nager.at");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddTransient<ITollFeeRepository, TollFeeRepository>();
        return services;
    }
}