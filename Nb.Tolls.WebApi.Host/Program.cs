using System.Text.Json;
using System.Text.Json.Serialization;
using Nb.Tolls.Application.Registrations;
using Nb.Tolls.Infrastructure.Registrations;
using Nb.Tolls.WebApi.Host.Validators;
using Nb.Tolls.WebApi.Host.Validators.Implementation;
using Nb.Tolls.WebApi.Host.Swagger;

namespace Nb.Tolls.WebApi.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();


        builder.Services
            .AddControllers()
            .AddJsonOptions(
                o =>
                {
                    o.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
                });
        
        builder.Services.AddSwagger();

        builder.Services.AddScoped<ITollFeesRequestValidator, TollFeesFeesRequestValidator>();
        builder.Services.AddTollsApplication();
        builder.Services.AddTollsInfrastructure(builder.Configuration);
        
        var app = builder.Build();
        
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();
        
        await app.RunAsync();
    }
}