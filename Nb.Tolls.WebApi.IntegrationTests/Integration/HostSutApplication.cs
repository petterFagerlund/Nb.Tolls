using System.Net.Http.Headers;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Nb.Tolls.Application.Services;
using Xunit;
using Xunit.Abstractions;
using Program = Nb.Tolls.WebApi.Host.Program;

namespace Nb.Tolls.WebApi.IntegrationTests.Integration;

[CollectionDefinition("API collection")]
public class ApiCollection : ICollectionFixture<HostSutApplication>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class HostSutApplication
{
    public HttpClient Client { get; private set; } = null!;
    public ITestOutputHelper Output { get; set; } = null!;
    public ITollFeesService TollFeesService { get; private set; } = null!;

    public void InitApplication()
    {
        TollFeesService = A.Fake<ITollFeesService>();

        var hostApplication = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Integration.Test");

                builder.ConfigureTestServices(services =>
                {
                    services.AddTransient(_ => TollFeesService);
                });

                builder.ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddXUnit(Output);
                });
            });

        var client = hostApplication.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
        Client = client;
    }
}