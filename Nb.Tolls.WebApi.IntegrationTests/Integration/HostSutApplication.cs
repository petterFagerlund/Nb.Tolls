using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;
using Xunit.Abstractions;

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

    public void InitApplication()
    {
        var hostApplication = new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder =>
            {
                builder.UseEnvironment("Integration.Test");
                builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders().AddXUnit(Output));

            });
        var client = hostApplication.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
        Client = client;
    }
}