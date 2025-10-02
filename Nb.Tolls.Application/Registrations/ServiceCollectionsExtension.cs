using Microsoft.Extensions.DependencyInjection;
using Nb.Tolls.Application.Services;
using Nb.Tolls.Application.Services.Implementations;

namespace Nb.Tolls.Application.Registrations;

public static class ServiceCollectionsExtension
{
    public static void AddTollsApplication(this IServiceCollection services)
    {
        services.AddTransient<ITollFeesCalculatorService, TollFeesCalculatorService>();
        services.AddTransient<ITollDateService, TollDateService>();
        services.AddTransient<ITollTimeService, TollTimeService>();
    }
}