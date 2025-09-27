using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nb.Tolls.Application.Registrations;
using Nb.Tolls.Infrastructure.Registrations;
using Nb.Tolls.WebApi.Host.Validators;
using Nb.Tolls.WebApi.Host.Validators.Implementation;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    });

builder.Services.AddScoped<ITollRequestValidator, TollRequestValidator>();
builder.Services.AddTollsApplication();
builder.Services.AddTollsInfrastructure();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
