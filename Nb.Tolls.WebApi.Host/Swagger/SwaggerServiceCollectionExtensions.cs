using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Nb.Tolls.WebApi.Host.Filters;
using Nb.Tolls.WebApi.Host.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Nb.Tolls.WebApi.Host.Swagger;

public static class SwaggerServiceCollectionExtensions
{
    public static void AddSwagger(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(
            options =>
            {
                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter token",
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        BearerFormat = "JWT",
                        Scheme = "bearer"
                    });
                options.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                options.OperationFilter<SwaggerDefaultValues>();
                options.SchemaFilter<EnumSchemaFilter>();
            });
    }
}