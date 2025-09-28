namespace Nb.Tolls.WebApi.Host.Swagger;

public static class SwaggerWebApplicationExtensions
{
    public static void UseSwagger(this WebApplication app)
    {
        app
            .UseSwagger(options =>
            {
                options.RouteTemplate = "api/norion/{documentName}/swagger.json";
            })
            .UseSwaggerUI(options =>
            {
                options.RoutePrefix = "api/norion";
                options.SwaggerEndpoint("/api/norion/v1/swagger.json", "Nb.Tolls API");
            });
    }
}